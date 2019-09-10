namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open System
open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.ExpressionSelection
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.Util

[<AutoOpen>]
module FSharpErrors =
    // https://github.com/fsharp/FSharp.Compiler.Service/blob/9.0.0/src/fsharp/CompileOps.fs#L246
    // https://github.com/fsharp/FSharp.Compiler.Service/blob/9.0.0/src/fsharp/FSComp.txt
    let [<Literal>] UnitTypeExpected = 20
    let [<Literal>] RuleNeverMatched = 26
    let [<Literal>] UndefinedName = 39
    let [<Literal>] UpcastUnnecessary = 66
    let [<Literal>] ModuleOrNamespaceRequired = 222
    let [<Literal>] UnrecognizedOption = 243
    let [<Literal>] UseBindingsIllegalInImplicitClassConstructors = 523
    let [<Literal>] LetAndForNonRecBindings = 576
    let [<Literal>] FieldRequiresAssignment = 764
    let [<Literal>] EmptyRecordInvalid = 789
    let [<Literal>] UseBindingsIllegalInModules = 524
    let [<Literal>] LocalClassBindingsCannotBeInline = 894
    let [<Literal>] UnusedValue = 1182
    let [<Literal>] UnusedThisVariable = 1183

[<AbstractClass>]
type FcsErrorsStageProcessBase(fsFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let document = daemonProcess.Document

    let getDocumentRange (error: FSharpErrorInfo) =
        if error.StartLineAlternate = 0 || error.ErrorNumber = ModuleOrNamespaceRequired then
            DocumentRange(document, TextRange(0, document.GetLineEndOffsetWithLineBreak(Line.O)))
        else
            let startOffset = getDocumentOffset document (docCoords error.StartLineAlternate error.StartColumn)
            let endOffset = getDocumentOffset document (docCoords error.EndLineAlternate error.EndColumn)
            DocumentRange(document, TextRange(startOffset, endOffset))

    let createGenericHighlighting (error: FSharpErrorInfo) range: IHighlighting =
        match error.Severity with
        | FSharpErrorSeverity.Warning -> WarningHighlighting(error.Message, range) :> _
        | _ -> ErrorHighlighting(error.Message, range) :> _

    let getNode range =
        getNode fsFile range

    let createHighlighting (error: FSharpErrorInfo) (range: DocumentRange): IHighlighting =

        match error.ErrorNumber with
        | UndefinedName ->
            let id = fsFile.GetNode<FSharpIdentifierToken>(range)
            if isNull id then UnresolvedHighlighting(error.Message, range) :> _ else

            let refExpr = id.Parent.As<IReferenceExpr>()
            if isNotNull refExpr || refExpr.Identifier == id then
                UndefinedNameError(refExpr.Reference, error.Message) :> _
            else
                UnresolvedHighlighting(error.Message, range) :> _

        | UpcastUnnecessary -> UpcastUnnecessaryWarning(getNode range) :> _

        | UnusedValue ->
            match fsFile.GetNode(range) with
            | null -> UnusedHighlighting(error.Message, range) :> _
            | pat -> UnusedValueWarning(pat) :> _

        | RuleNeverMatched ->
            match fsFile.GetNode(range) with
            | null -> createGenericHighlighting error range
            | matchClause -> RuleNeverMatchedWarning(matchClause) :> _

        | UnitTypeExpected ->
            UnitTypeExpectedWarning(ExpressionSelectionUtil.GetExpressionInRange(fsFile, range, false, null), error.Message) :> _
        
        | UseBindingsIllegalInModules ->
            UseBindingsIllegalInModulesWarning(getNode range) :> _

        | UseBindingsIllegalInImplicitClassConstructors ->
            UseKeywordIllegalInPrimaryCtorError(getNode range) :> _

        | LocalClassBindingsCannotBeInline ->
            LocalClassBindingsCannotBeInlineError(getNode range) :> _

        | LetAndForNonRecBindings ->
            LetAndForNonRecBindingsError(getNode range) :> _

        | UnusedThisVariable ->
            UnusedThisVariableWarning(getNode range) :> _

        | FieldRequiresAssignment ->
            FieldRequiresAssignmentError(getNode range, error.Message) :> _

        | EmptyRecordInvalid ->
            EmptyRecordInvalidError(getNode range, error.Message) :> _
        
        | _ -> createGenericHighlighting error range

    abstract ShouldAddDiagnostic: error: FSharpErrorInfo * range: DocumentRange -> bool
    default x.ShouldAddDiagnostic(error: FSharpErrorInfo, _) =
        error.ErrorNumber <> UnrecognizedOption

    member x.Execute(errors: FSharpErrorInfo[], committer: Action<DaemonStageResult>) =
        let highlightings = List(errors.Length)
        let errors =
            errors
            |> Array.map (fun error -> (error, getDocumentRange error))
            |> Array.distinctBy (fun (error, range) -> range, error.Message)

        for error, range in errors  do
            if x.ShouldAddDiagnostic(error, range) then
                let highlighting = createHighlighting error range
                highlightings.Add(HighlightingInfo(highlighting.CalculateRange(), highlighting))
            x.SeldomInterruptChecker.CheckForInterrupt()

        committer.Invoke(DaemonStageResult(highlightings))
