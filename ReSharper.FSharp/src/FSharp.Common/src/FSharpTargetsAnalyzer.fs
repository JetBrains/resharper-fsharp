namespace JetBrains.ReSharper.Plugins.FSharp.Common

open System
open JetBrains.Application
open JetBrains.ProjectModel.ProjectsHost.Diagnostic
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Diagnostic
open JetBrains.ReSharper.Host.Features.BackgroundTasks
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.Util
open JetBrains.Util.Logging

type NotificationHelper = RiderContextNotificationHelper

[<ShellComponent>]
type FSharpProjectLoadTargetsAnalyzer() =
         let diagnostic = FSharpTargetsDiagnosticMessage()

         interface IMsBuildProjectLoadDiagnosticProvider with
             member x.CollectDiagnostic(projectMark, _, result) =
                 match result.FatalError, projectMark with
                 | NotNull, FSharpProjectMark -> [ diagnostic :> ILoadDiagnostic ].AsCollection()
                 | _ -> EmptyList.Instance :> _

and FSharpTargetsDiagnosticMessage private (title, message) =
    inherit LoadDiagnosticMessage(title, message)
    new() =
        let messageTitle = "Could not open F# project"
        let osName =
            match PlatformUtil.RuntimePlatform with
            | PlatformUtil.Platform.Windows -> "windows"
            | PlatformUtil.Platform.Linux -> "linux"
            | PlatformUtil.Platform.MacOsX -> "mac"
            | _ -> Logger.GetLogger<FSharpProjectLoadTargetsAnalyzer>().Error("Unknown runtime platfrom"); String.Empty
        let installLink = NotificationHelper.MakeLink(sprintf "http://fsharp.org/use/%s/" osName, "install F# SDK")
        let message = "F# SDK or project dependencies are missing. " + 
                      sprintf "Try restoring NuGet packages; if the problem persists, please %s." installLink
        FSharpTargetsDiagnosticMessage(messageTitle, message)
