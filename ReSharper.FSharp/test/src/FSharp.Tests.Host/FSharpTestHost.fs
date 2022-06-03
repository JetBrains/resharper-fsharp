namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Host

open System
open System.Collections.Generic
open System.Globalization
open System.Linq
open FSharp.Compiler.IO
open JetBrains.Application.Notifications
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet.DotNetTools
open JetBrains.Rd.Tasks
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<SolutionComponent>]
type FSharpTestHost(solution: ISolution, sourceCache: FSharpSourceCache, itemsContainer: FSharpItemsContainer,
                    fantomasHost: FantomasHost, dotnetToolsTracker: NuGetDotnetToolsTracker, notifications: UserNotifications) =

    let lifetime = solution.GetLifetime()
    
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

        solution.GetComponent<IFcsProjectProvider>().GetProjectOptions(sourceFile)
        |> Option.map (fun options ->
            options.OtherOptions
            |> Array.choose (fun o -> if o.StartsWith("-r:") then Some (o.Substring("-r:".Length)) else None)
            |> Array.map (fun p -> VirtualFileSystemPath.TryParse(p, InteractionContext.SolutionContext))
            |> Array.filter (fun p -> not p.IsEmpty && directory.IsPrefixOf(p))
            |> Array.map (fun p -> p.Name)
            |> List)
        |> Option.defaultWith (fun _ -> List())

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

    do
        let fsTestHost = solution.RdFSharpModel().FsharpTestHost

        fsTestHost.GetLastModificationStamp.Set(FileSystem.GetLastWriteTimeShim)
        fsTestHost.GetSourceCache.Set(sourceCache.GetRdFSharpSource)
        fsTestHost.DumpSingleProjectMapping.Set(dumpSingleProjectMapping)
        fsTestHost.DumpSingleProjectLocalReferences.Set(dumpSingleProjectLocalReferences)
        fsTestHost.FantomasVersion.Set(fantomasVersion)
        fsTestHost.TypeProvidersRuntimeVersion.Set(typeProvidersRuntimeVersion)
        fsTestHost.DumpTypeProvidersProcess.Set(dumpTypeProvidersProcess)
        fsTestHost.GetCultureInfoAndSetNew.Set(getCultureInfoAndSetNew)
        fsTestHost.DumpFantomasRunOptions.Set(dumpFantomasRunOptions)
        fsTestHost.TerminateFantomasHost.Set(terminateFantomasHost)
        dotnetToolsTracker.DotNetToolCache.Change.Advise(lifetime, fun _ -> fsTestHost.DotnetToolInvalidated())
        notifications.AllNotifications.AddRemove.Property.Change.Advise(lifetime, fun x -> if x.HasNew && isNotNull x.New then fsTestHost.FantomasNotificationFired(formatNotifications x.New.Value))
