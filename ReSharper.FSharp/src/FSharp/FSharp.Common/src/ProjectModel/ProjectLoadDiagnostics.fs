namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Diagnostic

open System
open System.Collections.Generic
open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.HabitatDetector
open JetBrains.Platform.MsBuildHost.Models
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.Diagnostic
open JetBrains.ProjectModel.ProjectsHost.Impl
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Diagnostic
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.RdBackend.Common.Features.BackgroundTasks
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.Util

[<ShellComponent>]
[<ZoneMarker(typeof<IHostSolutionZone>)>]
type FSharpProjectLoadTargetsAnalyzer() =
     interface IMsBuildProjectLoadDiagnosticProvider with
         member x.CollectDiagnostic(projectMark, project) =
             let fatalError = project.FatalError
             if isNull fatalError then [||] else

             match fatalError.Message with
             | :? RdMsBuildUserMessage as message when message.PresentableText.Contains("Microsoft.FSharp.Targets") ->
                 [| FSharpTargetsDiagnosticMessage(projectMark) :> ProjectLoadDiagnostic |] :> _
             | _ -> EmptyArray.Instance :> _

and FSharpTargetsDiagnosticMessage(projectMark, message) =
    inherit ProjectLoadError(projectMark, message)

    static let platformName platform =
        match platform with
        | JetPlatform.MacOsX -> "mac"
        | JetPlatform.Linux -> "linux"
        | _ -> "windows"

    new (projectMark: IProjectMark) =
        let osName = platformName PlatformUtil.RuntimePlatform
        let url = $"https://fsharp.org/use/%s{osName}/"
        let link = RiderContextNotificationHelper.MakeLink(url, "install F# SDK")
        let message = "F# SDK or project dependencies are missing. " +
                      $"Try restoring NuGet packages; if the problem persists, please %s{link}."
        FSharpTargetsDiagnosticMessage(projectMark, message)


[<ShellComponent>]
[<ZoneMarker(typeof<IHostSolutionZone>)>]
type FSharpProjectTypeGuidAnalyzer() =
    interface IMsBuildProjectLoadDiagnosticProvider with
        member x.CollectDiagnostic(projectMark, _) =
            // Don't check guid when opening a single project or directory, i.e. not a solution file.
            if projectMark :? VirtualProjectMark then EmptyArray.Instance :> _ else

            if projectMark.Location.ExtensionNoDot <> FsprojExtension then EmptyArray.Instance :> _ else
            if isFSharpGuid projectMark.TypeGuid || projectMark.Guid = Guid.Empty then EmptyArray.Instance :> _ else

            [| FSharpWrongProjectTypeGuid(projectMark) :> ProjectLoadDiagnostic |] :> ICollection<_>

and FSharpWrongProjectTypeGuid(projectMark, message) =
    inherit ProjectLoadWarning(projectMark, message) // todo: allow passing custom title?

    static let [<Literal>] messageTitle = "F# project has incorrect guid"
    static let [<Literal>] message = "Solution file specifies wrong project type guid."

    new (projectMark: IProjectMark) =
        FSharpWrongProjectTypeGuid(projectMark, message)
