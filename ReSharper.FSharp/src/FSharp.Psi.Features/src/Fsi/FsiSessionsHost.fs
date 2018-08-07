namespace rec JetBrains.ReSharper.Plugins.FSharp.Services.ContextActions

open System
open System.Collections.Generic
open System.Globalization
open System.Text
open System.Threading
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.Platform.RdFramework.Util
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Host.Features.Toolset
open JetBrains.ReSharper.Plugins.FSharp.Services.Settings.Fsi
open JetBrains.Rider.Model
open JetBrains.Util

[<SolutionComponent>]
type FsiSessionsHost
        (lifetime: Lifetime, solution: ISolution, fsiDetector: IFsiDetector, fsiOptions: FsiOptionsProvider) =

    let stringArg option arg = sprintf "--%s:%O" option arg
    let boolArg option arg = sprintf "--%s%s" option (if arg then "+" else "-")

    let getNewFsiSessionInfo _ =
        let fsiPath = fsiDetector.GetSystemFsiDirectoryPath().FullPath
        let args =
            [| if PlatformUtil.IsRunningUnderWindows then
                   yield stringArg "fsi-server-output-codepage" Encoding.UTF8.CodePage
                   yield stringArg "fsi-server-input-codepage"  Encoding.UTF8.CodePage

               yield stringArg "fsi-server-lcid" Thread.CurrentThread.CurrentUICulture.LCID
               yield boolArg "shadowcopyreferences" fsiOptions.ShadowCopyReferences.Value

               yield! fsiOptions.FsiArgs.Value.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) |]
        RdFsiSessionInfo(fsiPath, List(args), fsiOptions.FixOptionsForDebug.Value)

    do
        let rdFsiHost = solution.GetProtocolSolution().GetRdFSharpModel().FSharpInteractiveHost
        rdFsiHost.RequestNewFsiSessionInfo.Set(getNewFsiSessionInfo)

        fsiOptions.MoveCaretOnSendLine.FlowInto(lifetime, rdFsiHost.MoveCaretOnSendLine)
        fsiOptions.ExecuteRecents.FlowInto(lifetime, rdFsiHost.CopyRecentToEditor)


[<SolutionComponent>]
type FsiDetector(toolset: RiderSolutionToolset, fsiOptions: FsiOptionsProvider) =
    interface IFsiDetector with
        member x.GetSystemFsiDirectoryPath() =
            // todo: also check:
            //   * Common7/IDE/CommonExtensions/Microsoft/FSharp per VS installation
            //   * FSharp.Compiler.Tools package if installed in solution
            //   * FSharp.Compiler.Tools package in nuget caches

            let fsiName = FsiOptions.getFsiName fsiOptions.UseAnyCpuVersion.Value
            if PlatformUtil.IsRunningOnMono then
                toolset.CurrentMonoRuntime.RootPath / "bin" / fsiName
            else
                let fsSDKsDir = PlatformUtils.GetProgramFiles86() / "Microsoft SDKs" / "F#"
                fsSDKsDir.GetChildDirectories()
                |> Seq.choose (fun path ->
                    match Double.TryParse(path.Name, NumberStyles.Any, CultureInfo.InvariantCulture) with
                    | true, version ->
                        let fsiPath = path / "Framework" / "v4.0" / fsiName
                        if fsiPath.ExistsFile then Some (version, fsiPath) else None
                    | _ -> None)
                |> Seq.sortBy fst
                |> Seq.tryHead
                |> Option.map snd
                |> Option.defaultValue FileSystemPath.Empty


type IFsiDetector =
    abstract GetSystemFsiDirectoryPath: unit -> FileSystemPath
