namespace JetBrains.ReSharper.Plugins.FSharp.Services.ContextActions

open JetBrains.Application.Settings
open JetBrains.DataFlow
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.ReSharper.Feature.Services.Resources
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
open System.IO
open System.Linq
open System.Text
open System.Threading

type SendToFsiActionType =
    | SendLine
    | SendSelection

[<SolutionComponent>]
type FsiSessionsHost(lifetime : Lifetime, solution : ISolution, solutionModel : SolutionModel) =
    let useFsiAnyCpu = solution.GetSettingsStore().GetValue(fun (s : FsiOptions) -> s.UseAnyCpuVersion)
    let fsiName = if useFsiAnyCpu then "fsharpiAnyCpu" else "fsharpi"
    let cultureId = Thread.CurrentThread.CurrentUICulture.LCID

    // todo: update on settings change
    let fsiPath =
        if PlatformUtil.IsRunningUnderWindows then
            let programFilesPath = FileSystemPath.TryParse(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
            let prefixPath = programFilesPath.Combine("Microsoft SDKs\\F#")
            let fsharpDir =
                ["4.1"; "4.0"; "3.1"; "3.0"]
                |> List.map (fun v -> prefixPath.Combine(v).Combine("Framework\\v4.0"))
                |> List.tryFind (fun p -> p.ExistsDirectory)
                |> Option.defaultValue FileSystemPath.Empty
            fsharpDir.Combine(fsiName + ".exe").FullPath
        else PlatformUtil.GetCurrentMonoRootDir().Combine(fsiName).FullPath

    let fsiHost =
        // todo: use TryGetSolution (currently internal)
        match solutionModel.Solutions.Values.LastOrDefault() with
        | null -> failwith "Could not get fsi host"
        | solution -> solution.FSharpInteractiveHost

    member x.SendText(text) = fsiHost.PrintOutput.Fire(text)

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

        Action<_>(fun _ -> solution.GetComponent<FsiSessionsHost>().SendText(visibleText))

[<ContextAction(Group = "F#", Name = "Send to F# Interactive", Description = "Sends F# code to F# Interactive session.")>]
type SendToFsiContextAction(dataProvider : FSharpContextActionDataProvider) =
    let icon = BulbThemedIcons.GhostBulb.Id
    let anchor = IntentionsAnchors.ContextActionsAnchor

    interface IContextAction with
        member x.IsAvailable(_) = true
        member x.CreateBulbItems() = seq { yield IntentionAction(SendToFsiAction(dataProvider), icon, anchor) }
