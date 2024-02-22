namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Host

#nowarn "57"

open System.Collections.Generic
open System.Globalization
open System.Linq
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.IO
open JetBrains.Application.Notifications
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet.DotNetTools
open JetBrains.Rd.Tasks
open JetBrains.RdBackend.Common.Env
open JetBrains.RdBackend.Common.Features.ProjectModel.View
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<SolutionComponent>]
[<ZoneMarker(typeof<IResharperHostCoreFeatureZone>, typeof<IFSharpPluginZone>)>]
type FSharpTestHost(solution: ISolution, sourceCache: FSharpSourceCache, itemsContainer: FSharpItemsContainer,
        fantomasHost: FantomasHost, dotnetToolsTracker: SolutionDotnetToolsTracker, notifications: UserNotifications,
        assemblyReaderShim: IFcsAssemblyReaderShim, projectProvider: IFcsProjectProvider,
        psiModules: IPsiModules, projectModelViewHost: ProjectModelViewHost, fsOptionsProvider: FSharpOptionsProvider) =

    let lifetime = solution.GetSolutionLifetimes().UntilSolutionCloseLifetime

    let dumpSingleProjectMapping _ =
        let projectMapping =
            itemsContainer.ProjectMappings.Values.SingleOrDefault().NotNull("Expected single project mapping.")
        projectMapping.DumpToString()

    let dumpSingleProjectLocalReferences _ =
        use cookie = ReadLockCookie.Create()

        let project = solution.GetAllProjects().Single(fun project -> not project.ProjectFileLocation.IsEmpty)
        let directory = project.Location.Directory

        let psiModule = solution.GetPsiServices().Modules.GetPsiModules(project).Single()

        let sourceFile =
            psiModule.SourceFiles
            |> Seq.find (fun sourceFile -> sourceFile.LanguageType.Is<FSharpProjectFileType>())

        projectProvider.GetProjectSnapshot(sourceFile)
        |> Option.map (fun options ->
            options.OtherOptions
            |> List.choose (fun o -> if o.StartsWith("-r:") then Some (o.Substring("-r:".Length)) else None)
            |> List.map (fun p -> VirtualFileSystemPath.TryParse(p, InteractionContext.SolutionContext))
            |> List.filter (fun p -> not p.IsEmpty && directory.IsPrefixOf(p))
            |> List.map (fun p -> p.Name)
            |> List)
        |> Option.defaultWith (fun _ -> List())

    let getProjectOptions (projectModelId: int) =
        use cookie = ReadLockCookie.Create()

        let project = projectModelViewHost.GetItemById<IProject>(projectModelId)
        let psiModule = psiModules.GetPsiModules(project) |> Seq.exactlyOne
        projectProvider.GetProjectSnapshot(psiModule).Value

    let dumpFcsProjectStamp (projectModelId: int) =
        let projectOptions = getProjectOptions projectModelId
        projectOptions.Stamp.Value

    let dumpFcsProjectReferences (projectModelId: int) =
        let projectOptions = getProjectOptions projectModelId
        projectOptions.ReferencedProjects
        |> List.map (fun project ->
            let outputPath = VirtualFileSystemPath.Parse(project.OutputFile, InteractionContext.SolutionContext)
            outputPath.NameWithoutExtension)
        |> List

    let dumpFcsModuleReader _ = assemblyReaderShim.TestDump

    let fantomasVersion _ = fantomasHost.Version()
    let dumpFantomasRunOptions _ = fantomasHost.DumpRunOptions()      
    let terminateFantomasHost _ =
        fantomasHost.Terminate()
        JetBrains.Core.Unit.Instance

    let typeProvidersRuntimeVersion _ =
        solution.GetComponent<IProxyExtensionTypingProvider>().RuntimeVersion()

    let dumpTypeProvidersProcess _ =
        solution.GetComponent<IProxyExtensionTypingProvider>().DumpTypeProvidersProcess()

    let getCultureInfoAndSetNew (culture: string) =
        let currentCulture = CultureInfo.CurrentUICulture
        let newCulture = CultureInfo.GetCultureInfo(culture)
        CultureInfo.DefaultThreadCurrentUICulture <- newCulture
        CultureInfo.CurrentUICulture <- newCulture
        currentCulture.Name

    let formatNotifications (notification: INotification) =
        $"""
----------------------------------------------------
Title: {notification.Title}
Body: {notification.Body}
Actions: {notification.AdditionalCommands
         |> Seq.map (fun x -> x.Title)
         |> String.concat ", "}
----------------------------------------------------
        """

    let updateAssemblyReaderSettings _ =
        fsOptionsProvider.UpdateAssemblyReaderSetting()
        JetBrains.Core.Unit.Instance

    do
        let fsTestHost = solution.RdFSharpModel().FsharpTestHost

        fsTestHost.GetLastModificationStamp.Set(FileSystem.GetLastWriteTimeShim)
        fsTestHost.GetSourceCache.Set(sourceCache.GetRdFSharpSource)
        fsTestHost.DumpSingleProjectMapping.Set(dumpSingleProjectMapping)
        fsTestHost.DumpSingleProjectLocalReferences.Set(dumpSingleProjectLocalReferences)
        fsTestHost.DumpFcsProjectStamp.Set(dumpFcsProjectStamp)
        fsTestHost.DumpFcsReferencedProjects.Set(dumpFcsProjectReferences)
        fsTestHost.DumpFcsModuleReader.Set(dumpFcsModuleReader)
        fsTestHost.FantomasVersion.Set(fantomasVersion)
        fsTestHost.TypeProvidersRuntimeVersion.Set(typeProvidersRuntimeVersion)
        fsTestHost.DumpTypeProvidersProcess.Set(dumpTypeProvidersProcess)
        fsTestHost.GetCultureInfoAndSetNew.Set(getCultureInfoAndSetNew)
        fsTestHost.DumpFantomasRunOptions.Set(dumpFantomasRunOptions)
        fsTestHost.TerminateFantomasHost.Set(terminateFantomasHost)
        fsTestHost.UpdateAssemblyReaderSettings.Set(updateAssemblyReaderSettings)
        dotnetToolsTracker.DotNetToolCache.Change.Advise(lifetime, fun _ -> fsTestHost.DotnetToolInvalidated())

        notifications.AllNotifications.AddRemove.Property.Change.Advise(lifetime, fun x ->
            if x.HasNew && isNotNull x.New then fsTestHost.FantomasNotificationFired(formatNotifications x.New.Value))
