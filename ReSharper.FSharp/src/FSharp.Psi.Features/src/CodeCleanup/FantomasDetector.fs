namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System.Collections.Generic
open System.Reflection
open JetBrains.Collections.Viewable
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet.DotNetTools
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.Threading
open JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
open NuGet.Versioning

[<AutoOpen>]
module private FantomasVersions =
    let [<Literal>] MinimalSupportedVersion = "3.2"
    let BundledVersion =
        Assembly.GetCallingAssembly().GetCustomAttribute<FantomasBundledVersionAttribute>().NotNull().Value

type FantomasValidationResult =
    | Ok
    | FailedToRun
    | UnsupportedVersion
    | SelectedButNotFound

type FantomasLocation =
    | Bundled
    | LocalDotnetTool
    | GlobalDotnetTool

type FantomasRunSettings =
    { Location: FantomasLocation
      Version: string
      Path: VirtualFileSystemPath
      mutable Status: FantomasValidationResult }

type FantomasDiagnosticNotification =
    { Event: FantomasValidationResult
      Location: FantomasLocation
      FallbackLocation: FantomasLocation }

[<SolutionInstanceComponent>]
type FantomasDetector(lifetime, fantomasSettingsProvider: FSharpFantomasSettingsProvider,
                      dotnetToolsTracker: NuGetDotnetToolsTracker) =
    let [<Literal>] fantomasToolPackageId = "fantomas-tool"
    let [<Literal>] fantomasPackageId = "fantomas"
    let minimalSupportedVersion = NuGetVersion(MinimalSupportedVersion)
    let notificationSignal = Signal<FantomasDiagnosticNotification>()
    let rwLock = JetFastSemiReenterableRWLock()

    let notificationsFired = HashSet(3)

    let versionsData =
        let dict = Dictionary(3)
        dict[Bundled] <- { Location = Bundled; Version = BundledVersion; Path = null; Status = Ok }
        dict

    let mutable versionToRun = ViewableProperty(versionsData[Bundled])
    let mutable delayedNotifications = []

    let calculateVersionToRun location =
        let rec calculateVersionRec location warnings =
            let nextVersionToSearch =
                match location with
                | FantomasLocation.LocalDotnetTool -> FantomasLocation.GlobalDotnetTool
                | _ -> FantomasLocation.Bundled

            match versionsData.TryGetValue(location) with
            | true, { Status = Ok } -> location, warnings
            | true, { Status = status } ->
                let versionToRun, warnings = calculateVersionRec nextVersionToSearch warnings

                let warnings =
                    if notificationsFired.Contains(location) then warnings else
                    notificationsFired.Add(location) |> ignore
                    { Event = status; Location = location; FallbackLocation = versionToRun } :: warnings

                versionToRun, warnings
            | false, _ -> calculateVersionRec nextVersionToSearch warnings

        calculateVersionRec location []

    let rec calculateRunOptions setting =
        let searchStartLocation =
            match setting with
            | FantomasLocationSettings.AutoDetected
            | FantomasLocationSettings.LocalDotnetTool -> FantomasLocation.LocalDotnetTool
            | FantomasLocationSettings.GlobalDotnetTool -> FantomasLocation.GlobalDotnetTool
            | _ -> FantomasLocation.Bundled

        let version, warnings = calculateVersionToRun searchStartLocation
        versionsData[version], warnings

    let fireNotifications () =
        for notification in delayedNotifications do
            notificationSignal.Fire(notification)
        delayedNotifications <- []

    let recalculateState locationSetting =
        match locationSetting with
        | FantomasLocationSettings.LocalDotnetTool
        | FantomasLocationSettings.GlobalDotnetTool ->
            let fantomasLocation =
                match locationSetting with
                | FantomasLocationSettings.LocalDotnetTool -> LocalDotnetTool
                | _ -> GlobalDotnetTool
    
            if versionsData.ContainsKey(fantomasLocation) then () else
            versionsData.Add(fantomasLocation, { Location = fantomasLocation; Version = ""; Path = null; Status = SelectedButNotFound })

        | _ -> ()

        let version, warnings = calculateRunOptions locationSetting
        
        versionToRun.Value <- version
        delayedNotifications <- warnings

    let validate version (pathToExecutable: VirtualFileSystemPath) =
        match NuGetVersion.TryParseStrict(version) with
        | true, version when version >= minimalSupportedVersion ->
            if isNotNull pathToExecutable && pathToExecutable.ExistsDirectory then Ok else FailedToRun
        | _ -> UnsupportedVersion

    let invalidateDotnetTool toolLocation toolInfo =
        match toolInfo with
        | Some (version, pathToExecutable: VirtualFileSystemPath) ->
            match versionsData.TryGetValue(toolLocation) with
            | true, { Version = cachedVersion; Status = Ok } when version = cachedVersion -> ()
            | _ ->
                versionsData.Remove(toolLocation) |> ignore
                notificationsFired.Remove(toolLocation) |> ignore
                let fantomasDirectory = if isNull pathToExecutable then null else pathToExecutable.Directory
                let status = validate version fantomasDirectory
                versionsData.Add(toolLocation, { Location = toolLocation; Version = version; Path = fantomasDirectory; Status = status })
        | _ ->
            versionsData.Remove(toolLocation) |> ignore
            notificationsFired.Remove(toolLocation) |> ignore

    let invalidateDotnetTools (cache: DotNetToolCache) =
        cache.ToolLocalCache.GetAllLocalTools()
        |> Seq.tryFind (fun x -> x.PackageId = fantomasPackageId || x.PackageId = fantomasToolPackageId)
        |> Option.map (fun x -> x.Version, x.PathToExecutable)
        |> invalidateDotnetTool LocalDotnetTool

        let globalTools =
            match cache.ToolGlobalCache.GetGlobalTool(fantomasPackageId) with
            | null -> cache.ToolGlobalCache.GetGlobalTool(fantomasToolPackageId)
            | x -> x

        globalTools
        |> Option.ofObj
        // TODO: GlobalToolCacheEntry should be a single entry instead of a list
        |> Option.map (fun x -> x[0].Version.ToNormalizedString(), x[0].EntryPointPath)
        |> invalidateDotnetTool GlobalDotnetTool

    do
        // Required for use from welcome screen, before loading solution components.
        if isNull fantomasSettingsProvider || isNull dotnetToolsTracker then () else

        fantomasSettingsProvider.Location.Change.Advise(lifetime, fun x ->
            if not x.HasNew then () else
            use _ = rwLock.UsingWriteLock()
            recalculateState x.New)
        
        dotnetToolsTracker.DotNetToolCache.Change.Advise(lifetime, fun x ->
            if not x.HasNew || isNull x.New then () else
            use _ = rwLock.UsingWriteLock()
            let settingsVersion = fantomasSettingsProvider.Location.Value
            invalidateDotnetTools x.New
            recalculateState settingsVersion)

    static member Create(lifetime) =
        FantomasDetector(lifetime, Unchecked.defaultof<FSharpFantomasSettingsProvider>, Unchecked.defaultof<NuGetDotnetToolsTracker>)

    member x.TryRun(runAction: VirtualFileSystemPath -> unit) =
        use _ = rwLock.UsingWriteLock()
        fireNotifications()
        let { Path = path; Location = version; Version = _ } as versionToRun = versionToRun.Value
        try runAction path
        with _ ->
            versionToRun.Status <- FailedToRun
            match version with
            | Bundled ->
                notificationSignal.Fire({ Event = FailedToRun; Location = Bundled; FallbackLocation = Bundled })
                notificationsFired.Add(Bundled) |> ignore
            | _ ->
            recalculateState fantomasSettingsProvider.Location.Value
            x.TryRun(runAction)

    member x.GetSettings() =
        let settings = Dictionary()
        use _ = rwLock.UsingReadLock()

        for kvp in versionsData do
            let key =
                match kvp.Key with
                | FantomasLocation.Bundled -> FantomasLocationSettings.Bundled
                | FantomasLocation.LocalDotnetTool -> FantomasLocationSettings.LocalDotnetTool
                | FantomasLocation.GlobalDotnetTool -> FantomasLocationSettings.GlobalDotnetTool
            settings.Add(key, kvp.Value)

        let autoDetectedSettingsData, _ = calculateRunOptions FantomasLocationSettings.AutoDetected
        settings.Add(FantomasLocationSettings.AutoDetected, autoDetectedSettingsData)
        settings

    member x.VersionToRun = versionToRun
    member x.NotificationProducer = notificationSignal
