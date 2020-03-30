namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open FSharp.Compiler.Layout
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application
open JetBrains.Application.UI.Components
open JetBrains.Application.UI.PopupLayout
open JetBrains.Application.UI.Tooltips
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon.CodeInsights
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Host.Features.Services
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Model


[<StaticSeverityHighlighting(
    Severity.INFO,
    HighlightingGroupIds.CodeInsightsGroup, 
    AttributeId = HighlightingGroupIds.CodeInsightsGroup,
    OverlapResolve = OverlapResolveKind.NONE)>]
type FSharpPipeChainHighlighting(range, text, provider: ICodeInsightsProvider) =
    inherit CodeInsightsHighlighting(range, text, "", "Copy type", provider, null, null)

    interface IHighlightingWithTestOutput with
        member x.TestOutput = text


[<ShellComponent>]
type PipeChainCodeVisionProvider() =
    let [<Literal>] id = "F# |> chain types"
    let [<Literal>] copiedText = "Type copied to clipboard"

    interface ICodeInsightsProvider with
        member x.ProviderId = id
        member x.DisplayName = id
        member x.DefaultAnchor = CodeLensAnchorKind.Right
        member x.RelativeOrderings = [| CodeLensRelativeOrderingFirst() :> CodeLensRelativeOrdering |] :> _

        member x.IsAvailableIn _ = true

        member x.OnClick(highlighting, _) =
            let entry = highlighting.Entry.As<TextCodeLensEntry>()
            if isNull entry then () else

            let shell = Shell.Instance
            shell.GetComponent<Clipboard>().SetText(entry.Text)
            shell.GetComponent<ITooltipManager>().Show(copiedText, PopupWindowContextSource(fun _ ->
                let offset = highlighting.Range.StartOffset.Offset
                RiderEditorOffsetPopupWindowContext(offset) :> _)) |> ignore

        member x.OnExtraActionClick(_, _, _) = ()


[<DaemonStage>]
type PipeChainCodeVisionStage(provider: PipeChainCodeVisionProvider) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT && base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        PipeChainCodeVisionProviderProcess(fsFile, settings, daemonProcess, provider) :> _


and PipeChainCodeVisionProviderProcess(fsFile, settings, daemonProcess, provider: ICodeInsightsProvider) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let tokenNames = ["|>"]
    let [<Literal>] opName = "PipeChainCodeVisionProviderProcess"

    override x.Execute(committer) =
        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        fsFile.ProcessThisAndDescendants(Processor(x, consumer))
        committer.Invoke(DaemonStageResult(consumer.Highlightings))

    override x.VisitBinaryAppExpr(binding, consumer) =
        if binding.Operator.QualifiedName <> "|>" then () else

        let sourceFile = daemonProcess.SourceFile
        let opExpr = binding.Operator

        match sourceFile.GetPsiFile<FSharpLanguage>(opExpr.GetNavigationRange()).As<IFSharpFile>() with
        | null -> ()
        | fsFile ->

        match opExpr.Identifier.As<FSharpIdentifierToken>() with
        | null -> ()
        | token ->

        match fsFile.GetParseAndCheckResults(true, opName) with
        | None -> ()
        | Some results ->

        let checkResults = results.CheckResults
        let coords = sourceFile.Document.GetCoordsByOffset(token.GetTreeEndOffset().Offset)
        let lineText = sourceFile.Document.GetLineText(coords.Line)

        use cookie = CompilationContextCookie.GetOrCreate(fsFile.GetPsiModule().GetContextFromModule())

        // todo: provide tooltip for #r strings in fsx, should pass String tag
        let getTooltip = checkResults.GetStructuredToolTipText(int coords.Line + 1, int coords.Column, lineText, tokenNames, FSharpTokenTag.Identifier)
        let (FSharpToolTipText layouts) = getTooltip.RunAsTask()

        match layouts with
        | [ FSharpStructuredToolTipElement.Group [ overload ] ] ->
            match overload.TypeMapping with
            | [ _; result ] ->
                // TODO: do something way less hacky here
                // Trim off the: "'U is " prefix
                let text = ": " + (showL result).Substring(6)
                FSharpPipeChainHighlighting(binding.RightArgument.GetNavigationRange(), text, provider)
                |> consumer.AddHighlighting
            | _ -> ()
        | _ -> ()
