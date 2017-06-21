namespace JetBrains.ReSharper.Plugins.FSharp.Services.ContextActions

open JetBrains.Application.Settings
open JetBrains.DataFlow
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.ReSharper.Feature.Services.Resources
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Host.Features.Toolset
open JetBrains.ReSharper.Plugins.FSharp.Services.Settings
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.FSharp
open JetBrains.ReSharper.Psi.FSharp.Tree
open JetBrains.Rider.Model
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.Util
open JetBrains.Util.dataStructures.TypedIntrinsics
open System
open System.Diagnostics
open System.Collections.Generic
open System.IO
open System.Linq
open System.Text
open System.Threading

type SendToFsiActionType =
    | SendLine
    | SendSelection

[<SolutionComponent>]
type FsiSessionsHost(solution : ISolution, solutionModel : SolutionModel, toolset : RiderSolutionToolset) as this =
    let rdFsiHost =
        match solutionModel.TryGetCurrentSolution() with
        | null -> failwith "Could not get fsi host"
        | solution -> solution.FSharpInteractiveHost

    let stringOption option arg = sprintf "--%s:%O" option arg
    let boolOption option arg = sprintf "--%s%s" option (if arg then "+" else "-")

    do rdFsiHost.RequestNewFsiSessionInfo.Set(this.GetNewFsiSessionInfo)

    member x.GetNewFsiSessionInfo(_) =
        let settings = solution.GetSettingsStore()
        let useFsiAnyCpu = settings.GetValue(fun (s : FsiOptions) -> s.UseAnyCpuVersion)

        // todo: move discover process to another place (and discover other F#-specific things like targets files)
        let fsiPath =
            if PlatformUtil.IsRunningUnderWindows then
                let fsiName = if useFsiAnyCpu then "fsiAnyCpu.exe" else "fsi.exe"
                let programFilesPath = FileSystemPath.TryParse(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
                let prefixPath = programFilesPath.Combine("Microsoft SDKs\\F#")
                let fsharpDir =
                    ["4.1"; "4.0"; "3.1"]
                    |> List.map (fun v -> prefixPath.Combine(v).Combine("Framework\\v4.0"))
                    |> List.tryFind (fun p -> p.ExistsDirectory)
                    |> function | Some p -> p | _ -> FileSystemPath.Empty
                fsharpDir.Combine(fsiName).FullPath
            else
                let fsiName = if useFsiAnyCpu then "fsharpiAnyCpu" else "fsharpi"
                toolset.CurrentMonoRuntime.RootPath.Combine("bin").Combine(fsiName).FullPath

        let shadowCopyReferences = settings.GetValue(fun (s : FsiOptions) -> s.ShadowCopyReferences)
        let userArgs =
            settings.GetValue(fun (s : FsiOptions) -> s.FsiArgs).Split(' ')
            |> Seq.ofArray
            |> Seq.map (fun s -> s.Trim())
            |> Seq.filter (fun s -> not (s.IsEmpty()))
            
        let args =
            seq { 
                yield stringOption "fsi-server-output-codepage" Encoding.UTF8.CodePage
                yield stringOption "fsi-server-input-codepage"  Encoding.UTF8.CodePage
                yield stringOption "fsi-server-lcid" Thread.CurrentThread.CurrentUICulture.LCID
                yield boolOption "shadowcopyreferences" shadowCopyReferences
                yield! userArgs
            }
        RdFsiSessionInfo(fsiPath, List(args))

    member x.SendText(request) = rdFsiHost.SendText.Fire(request)

and FSharpContextActionDataProvider(solution : ISolution, textControl, file) =
    inherit CachedContextActionDataProviderBase<IFSharpFile>(solution, textControl, file)

[<ContextActionDataBuilder(typeof<FSharpContextActionDataProvider>)>]
type FSharpContextActionDataBuilder() =
    inherit ContextActionDataBuilderBase<FSharpLanguage, IFSharpFile>()
    override x.BuildFromPsi(solution, textControl, file) =
        FSharpContextActionDataProvider(solution, textControl, file) :> _

type SendToFsiAction(dataProvider : FSharpContextActionDataProvider) =
    inherit BulbActionBase()

    let actionType = if dataProvider.DocumentSelection.Length > 0 then SendSelection else SendLine

    override x.Text =
        match actionType with
        | SendLine -> "line"
        | SendSelection -> "selection"
        |> sprintf "Send %s to F# Interactive"

    override x.ExecutePsiTransaction(solution, _) =
        let filePath = dataProvider.SourceFile.GetLocation()
        let document = dataProvider.Document
        let startLine = document.GetCoordsByOffset(dataProvider.DocumentSelection.StartOffset.Offset).Line |> int

        let visibleText =
            match actionType with
            | SendLine -> document.GetLineText(document.GetCoordsByOffset(dataProvider.DocumentCaret.Offset).Line)
            | SendSelection -> dataProvider.DocumentSelection.GetText()

        // copied from visualfsharp
        let fsiText =
            sprintf "\n# silentCd @\"%s\" ;; \n" filePath.Directory.FullPath
          + sprintf "# %d @\"%s\" \n" startLine filePath.FullPath
          + visibleText + "\n# 1 \"stdin\"\n;;\n"

        Action<_>(fun _ -> solution.GetComponent<FsiSessionsHost>().SendText(RdFsiSendTextRequest(visibleText, fsiText)))

[<ContextAction(Group = "F#", Name = "Send to F# Interactive", Description = "Sends F# code to F# Interactive session.")>]
type SendToFsiContextAction(dataProvider : FSharpContextActionDataProvider) =
    let icon = BulbThemedIcons.GhostBulb.Id
    let anchor = IntentionsAnchors.ContextActionsAnchor

    interface IContextAction with
        member x.IsAvailable(_) = true
        member x.CreateBulbItems() = seq { yield IntentionAction(SendToFsiAction(dataProvider), icon, anchor) }
