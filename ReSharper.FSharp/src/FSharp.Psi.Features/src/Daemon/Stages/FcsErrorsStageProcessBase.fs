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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.Util

[<AutoOpen>]
module FSharpErrors =
    // https://github.com/fsharp/FSharp.Compiler.Service/blob/9.0.0/src/fsharp/CompileOps.fs#L246
    // https://github.com/fsharp/FSharp.Compiler.Service/blob/9.0.0/src/fsharp/FSComp.txt
    let [<Literal>] TypeEquation = 1
    let [<Literal>] UnitTypeExpected = 20
    let [<Literal>] RuleNeverMatched = 26
    let [<Literal>] VarBoundTwice = 38
    let [<Literal>] UndefinedName = 39
    let [<Literal>] UpcastUnnecessary = 66
    let [<Literal>] TypeTestUnnecessary = 67
    let [<Literal>] ModuleOrNamespaceRequired = 222
    let [<Literal>] UnrecognizedOption = 243
    let [<Literal>] UseBindingsIllegalInImplicitClassConstructors = 523
    let [<Literal>] LetAndForNonRecBindings = 576
    let [<Literal>] FieldRequiresAssignment = 764
    let [<Literal>] ExpectedExpressionAfterLet = 588
    let [<Literal>] SuccessiveArgsShouldBeSpacedOrTupled = 597
    let [<Literal>] EmptyRecordInvalid = 789
    let [<Literal>] UseBindingsIllegalInModules = 524
    let [<Literal>] LocalClassBindingsCannotBeInline = 894
    let [<Literal>] UnusedValue = 1182
    let [<Literal>] UnusedThisVariable = 1183

    let [<Literal>] MissingErrorNumber = 193

    let [<Literal>] undefinedIndexerMessage = "The field, constructor or member 'Item' is not defined."
    let [<Literal>] ifExprMissingElseBranch = "This 'if' expression is missing an 'else' branch."
    let [<Literal>] expressionIsAFunctionMessage = "This expression is a function value, i.e. is missing arguments. Its type is string -> unit."

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

    let createHighlightingFromNode highlightingCtor range: IHighlighting =
        match ExpressionSelectionUtil.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | expr -> highlightingCtor expr :> _

    let createHighlightingFromParentNode highlightingCtor range: IHighlighting =
        match ExpressionSelectionUtil.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | node -> highlightingCtor (node.GetContainingNode()) :> _
    
    let createHighlightingFromNodeWithMessage highlightingCtor range (error: FSharpErrorInfo): IHighlighting =
        let expr = ExpressionSelectionUtil.GetExpressionInRange(fsFile, range, false, null)
        if isNotNull expr then highlightingCtor (expr, error.Message) :> _ else
        null

    let createHighlighting (error: FSharpErrorInfo) (range: DocumentRange): IHighlighting =
        match error.ErrorNumber with
        | TypeEquation when error.Message.StartsWith(ifExprMissingElseBranch, StringComparison.Ordinal) ->
            createHighlightingFromNodeWithMessage UnitTypeExpectedError range error 

        | VarBoundTwice ->
            createHighlightingFromNode VarBoundTwiceError range

        | UndefinedName ->
            if (error.Message = undefinedIndexerMessage &&
                    let indexer = fsFile.GetNode(range) in isNotNull indexer) then
                UndefinedIndexerError(fsFile.GetNode(range)) :> _ else

            let identifier = fsFile.GetNode(range)

            let reference =
                let refExpr = ReferenceExprNavigator.GetByIdentifier(identifier)
                if isNotNull refExpr then refExpr.Reference else

                let referenceName = ReferenceNameNavigator.GetByIdentifier(identifier)
                if isNotNull referenceName then referenceName.Reference else null

            if isNotNull reference then UndefinedNameError(reference, error.Message) :> _ else

            UnresolvedHighlighting(error.Message, range) :> _

        | UpcastUnnecessary ->
            createHighlightingFromNode UpcastUnnecessaryWarning range

        | TypeTestUnnecessary ->
            createHighlightingFromNodeWithMessage TypeTestUnnecessaryWarning range error

        | UnusedValue ->
            match fsFile.GetNode(range) with
            | null -> UnusedHighlighting(error.Message, range) :> _
            | pat -> UnusedValueWarning(pat) :> _

        | RuleNeverMatched ->
            createHighlightingFromParentNode RuleNeverMatchedWarning range

        | UnitTypeExpected ->
            createHighlightingFromNodeWithMessage UnitTypeExpectedWarning range error
        
        | UseBindingsIllegalInModules ->
            createHighlightingFromNode UseBindingsIllegalInModulesWarning range

        | UseBindingsIllegalInImplicitClassConstructors ->
            createHighlightingFromNode UseKeywordIllegalInPrimaryCtorError range

        | LocalClassBindingsCannotBeInline ->
            createHighlightingFromParentNode LocalClassBindingsCannotBeInlineError range

        | LetAndForNonRecBindings ->
            createHighlightingFromParentNode LetAndForNonRecBindingsError range

        | UnusedThisVariable ->
            createHighlightingFromParentNode UnusedThisVariableWarning range

        | FieldRequiresAssignment ->
            createHighlightingFromNodeWithMessage FieldRequiresAssignmentError range error

        | ExpectedExpressionAfterLet ->
            createHighlightingFromParentNode ExpectedExpressionAfterLetError range

        | SuccessiveArgsShouldBeSpacedOrTupled ->
            createHighlightingFromNode SuccessiveArgsShouldBeSpacedOrTupledError range

        | EmptyRecordInvalid ->
            createHighlightingFromNodeWithMessage EmptyRecordInvalidError range error

        | MissingErrorNumber when startsWith expressionIsAFunctionMessage error.Message ->
            createHighlightingFromNodeWithMessage FunctionValueUnexpectedWarning range error

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
                let highlighting =
                    match createHighlighting error range with
                    | null -> createGenericHighlighting error range
                    | highlighting -> highlighting

                highlightings.Add(HighlightingInfo(highlighting.CalculateRange(), highlighting))
            x.SeldomInterruptChecker.CheckForInterrupt()

        committer.Invoke(DaemonStageResult(highlightings))
