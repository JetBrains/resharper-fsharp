namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open System
open System.Diagnostics
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Linq
open System.Text
open System.Threading
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.Platform.RdFramework.Base
open JetBrains.Platform.RdFramework.Util
open JetBrains.ProjectModel.BuildTools
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.ReSharper.Feature.Services.Resources
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Host.Features.Runtime
open JetBrains.ReSharper.Host.Features.Toolset
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Rider.Model
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.Util
open JetBrains.Util.dataStructures.TypedIntrinsics

[<ShellComponent>]
type FsiDetector(monoRuntimeDetector: MonoRuntimeDetector) =
    member x.GetSystemFsiDirectoryPath() =
        // todo: also check:
        //   * Common7\IDE\CommonExtensions\Microsoft\FSharp per VS installation
        //   * FSharp.Compiler.Tools package if installed in solution
        //   * FCT package in nuget caches?
        if PlatformUtil.IsRunningUnderWindows then
            let fsSdkDir = PlatformUtils.GetProgramFiles86() / "Microsoft SDKs" / "F#"
            fsSdkDir.GetChildDirectories()
            |> Seq.choose (fun path ->
                match Double.TryParse(path.Name, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, version ->
                    let sdkPath = path / "Framework" / "v4.0"
                    if not sdkPath.ExistsDirectory then None else
                    Some (version, sdkPath)
                | _ -> None)
            |> Seq.sortByDescending fst
            |> Seq.tryHead
            |> Option.map snd
            |> Option.defaultValue FileSystemPath.Empty
        else
            let runtimes = monoRuntimeDetector.DetectMonoRuntimes()
            if runtimes.IsEmpty() then FileSystemPath.Empty else

            // todo
            runtimes.[0].RootPath / "bin"


[<SolutionComponent>]
type FsiSessionsHost(lifetime, solutionModel: SolutionModel, fsiDetector: FsiDetector, fsiOptions: FsiOptionsProvider) =
    let rdFsiHost =
        match solutionModel.TryGetCurrentSolution() with
        | null -> failwith "Could not get protocol solution"
        | solution -> solution.GetRdFSharpModel().FSharpInteractiveHost

    let getNewSessionInfo _ =
        let fsiPath =
            let fsiDirectory = fsiDetector.GetSystemFsiDirectoryPath()
            let fsiName = getFsiName fsiOptions.UseAnyCpuVersion.Value
            fsiDirectory / fsiName

        let args =
            let stringOption option arg = sprintf "--%s:%O" option arg
            let boolOption option arg = sprintf "--%s%s" option (if arg then "+" else "-")

            let userArgs =
                fsiOptions.FsiArgs.Value.Split(' ')
                |> Array.map (fun s -> s.Trim())
                |> Seq.filter (fun s -> not (s.IsEmpty()))

            seq { 
                if PlatformUtil.IsRunningUnderWindows then
                    yield stringOption "fsi-server-output-codepage" Encoding.UTF8.CodePage
                    yield stringOption "fsi-server-input-codepage"  Encoding.UTF8.CodePage

                yield stringOption "fsi-server-lcid" Thread.CurrentThread.CurrentUICulture.LCID
                yield boolOption "shadowcopyreferences" fsiOptions.ShadowCopyReferences.Value
                yield! userArgs
            }
        RdFsiSessionInfo(fsiPath.FullPath, List(args))

    do
        rdFsiHost.RequestNewFsiSessionInfo.Set(getNewSessionInfo)
        fsiOptions.MoveCaretOnSendLine.FlowInto(lifetime, rdFsiHost.MoveCaretOnSendLine)
        fsiOptions.ExecuteRecents.FlowInto(lifetime, rdFsiHost.CopyRecentToEditor)
