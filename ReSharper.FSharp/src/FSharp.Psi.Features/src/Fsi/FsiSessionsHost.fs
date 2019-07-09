namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open System
open System.Collections.Generic
open System.Text
open System.Threading
open JetBrains.Lifetimes
open JetBrains.Platform.RdFramework.Util
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.FsiDetector
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.Settings
open JetBrains.Rider.Model
open JetBrains.Util

[<SolutionComponent>]
type FsiSessionsHost
        (lifetime: Lifetime, solution: ISolution, fsiDetector: FsiDetector, fsiOptions: FsiOptionsProvider) =

    let stringArg = sprintf "--%s:%O"
    let boolArg option arg = sprintf "--%s%s" option (if arg then "+" else "-")

    let stringArrayArgs (arg: string) =
        arg.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)

    let getNewFsiSessionInfo _ =
        let fsi = fsiDetector.GetAutodetected(solution)

        let fsiPath =
            if fsiOptions.AutoDetect.Value then
                fsi.GetFsiPath(fsiOptions.UseAnyCpu.Value)
            else
                fsiOptions.FsiPathAsPath

        let args =
            [| yield! stringArrayArgs fsiOptions.FsiArgs.Value
               yield! stringArrayArgs fsiOptions.FsiInternalArgs.Value

               yield boolArg "shadowcopyreferences" fsiOptions.ShadowCopyReferences.Value

               if PlatformUtil.IsRunningUnderWindows then
                   yield stringArg "fsi-server-output-codepage" Encoding.UTF8.CodePage
                   yield stringArg "fsi-server-input-codepage"  Encoding.UTF8.CodePage

               yield stringArg "fsi-server-lcid" Thread.CurrentThread.CurrentUICulture.LCID |]

        RdFsiSessionInfo(fsiPath.FullPath, fsi.Runtime, fsi.IsCustom, List(args), fsiOptions.FixOptionsForDebug.Value)

    do
        let rdFsiHost = solution.GetProtocolSolution().GetRdFSharpModel().FSharpInteractiveHost
        rdFsiHost.RequestNewFsiSessionInfo.Set(getNewFsiSessionInfo)

        fsiOptions.MoveCaretOnSendLine.FlowInto(lifetime, rdFsiHost.MoveCaretOnSendLine)
        fsiOptions.ExecuteRecent.FlowInto(lifetime, rdFsiHost.CopyRecentToEditor)
