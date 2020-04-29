namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.ParameterNameHints
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText

type ParameterNameHintHighlightingProcess(fsFile, settings, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    override x.Execute(committer) =
        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        fsFile.ProcessThisAndDescendants(Processor(x, consumer))
        committer.Invoke(DaemonStageResult(consumer.Highlightings))

    override x.VisitPrefixAppExpr(prefixAppExpr, consumer) =
        let args =
            match prefixAppExpr.ArgumentExpression.IgnoreInnerParens() with
            | :? ITupleExpr as argExpression ->
                argExpression.Expressions
                |> Seq.toArray
            | expr -> [| expr |]

        for argExpr in args do
            match argExpr.IgnoreInnerParens() with
            | :? ILiteralExpr ->
                let arg = argExpr.As<IArgument>()
                // todo: is there ever a case where a literal expression isn't an IArgument? if not, can this be encoded in the type system?

                let param = arg.MatchingParameter
                if isNull param then () else

                let parameterName = param.Element.ShortName
                consumer.AddHighlighting(ParameterNameHintHighlighting(arg.GetDocumentRange(), param.Element, "", RichText(parameterName), fsFile.GetKnownLanguage(), ""))
            | _ -> ()

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type ParameterNameHintStage() =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT &&
        base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        // todo: hook into parameter name settings
        //if not (settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowInferredTypes)) then null else
        ParameterNameHintHighlightingProcess(fsFile, settings, daemonProcess) :> _
