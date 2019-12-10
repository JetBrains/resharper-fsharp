namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Diagnostic

open System
open System.Collections.Generic
open JetBrains.Application
open JetBrains.ProjectModel.ProjectsHost.Diagnostic
open JetBrains.ProjectModel.ProjectsHost.Impl
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Diagnostic
open JetBrains.ReSharper.Host.Features.BackgroundTasks
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.Util

[<ShellComponent>]
type FSharpProjectLoadTargetsAnalyzer() =
     interface IMsBuildProjectLoadDiagnosticProvider with
         member x.CollectDiagnostic(_, _, result) =
             match result.FatalError with
             | null -> EmptyArray.Instance :> _
             | error when error.PresentableTest.Contains("Microsoft.FSharp.Targets") ->
                 FSharpTargetsDiagnosticMessage.InstanceCollection
             | _ -> EmptyArray.Instance :> _

and FSharpTargetsDiagnosticMessage private (title, message) =
    inherit LoadDiagnosticMessage(title, message)

    static let [<Literal>] messageTitle = "Could not open F# project"

    static let platformName platform =
        match platform with
        | PlatformUtil.Platform.MacOsX -> "mac"
        | PlatformUtil.Platform.Linux -> "linux"
        | _ -> "windows"

    private new() =
        let osName = platformName PlatformUtil.RuntimePlatform
        let url = sprintf "https://fsharp.org/use/%s/" osName
        let link = RiderContextNotificationHelper.MakeLink(url, "install F# SDK")
        let message = "F# SDK or project dependencies are missing. " + 
                      sprintf "Try restoring NuGet packages; if the problem persists, please %s." link
        FSharpTargetsDiagnosticMessage(messageTitle, message)

    static member val InstanceCollection =
        [| FSharpTargetsDiagnosticMessage() :> ILoadDiagnostic |] :> ICollection<_>


[<ShellComponent>]
type FSharpProjectTypeGuidAnalyzer() =
    interface IMsBuildProjectLoadDiagnosticProvider with
        member x.CollectDiagnostic(projectMark, _, _) =
            // Don't check guid when opening a single project or directory, i.e. not a solution file.
            if projectMark :? VirtualProjectMark then EmptyArray.Instance :> _ else

            if projectMark.Location.ExtensionNoDot <> FsprojExtension then EmptyArray.Instance :> _ else
            if isFSharpGuid projectMark.TypeGuid || projectMark.Guid = Guid.Empty then EmptyArray.Instance :> _ else 

            FSharpWrongProjectTypeGuid.InstanceCollection

and FSharpWrongProjectTypeGuid private (title, message) =
    inherit LoadDiagnosticMessage(title, message)

    static let [<Literal>] messageTitle = "F# project has incorrect guid"
    static let [<Literal>] message = "Solution file specifies wrong project type guid."

    private new() = FSharpWrongProjectTypeGuid(messageTitle, message)

    static member val InstanceCollection =
        [| FSharpWrongProjectTypeGuid() :> ILoadDiagnostic |] :> ICollection<_>
