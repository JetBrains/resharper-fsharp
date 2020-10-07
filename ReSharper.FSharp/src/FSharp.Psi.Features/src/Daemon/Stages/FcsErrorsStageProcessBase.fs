namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open System
open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.Util

[<AutoOpen>]
module FSharpErrors =
    // Error numbers as reported by FCS:
    // * Exception types: fsharp/CompilerDiagnostics.fs#L217
    // * Generated from text: fsharp/FSComp.txt

    let [<Literal>] TypeEquation = 1
    let [<Literal>] NotAFunction = 3
    let [<Literal>] FieldNotMutable = 5
    let [<Literal>] UnitTypeExpected = 20
    let [<Literal>] MatchIncomplete = 25
    let [<Literal>] RuleNeverMatched = 26
    let [<Literal>] ValNotMutable = 27
    let [<Literal>] VarBoundTwice = 38
    let [<Literal>] UndefinedName = 39
    let [<Literal>] UpcastUnnecessary = 66
    let [<Literal>] TypeTestUnnecessary = 67
    let [<Literal>] EnumMatchIncomplete = 104
    let [<Literal>] NamespaceCannotContainValues = 201
    let [<Literal>] ModuleOrNamespaceRequired = 222
    let [<Literal>] UnrecognizedOption = 243
    let [<Literal>] NoImplementationGiven = 365
    let [<Literal>] NoImplementationGivenWithSuggestion = 366
    let [<Literal>] UseBindingsIllegalInImplicitClassConstructors = 523
    let [<Literal>] LetAndForNonRecBindings = 576
    let [<Literal>] FieldRequiresAssignment = 764
    let [<Literal>] ExpectedExpressionAfterLet = 588
    let [<Literal>] SuccessiveArgsShouldBeSpacedOrTupled = 597
    let [<Literal>] ConstructRequiresListArrayOrSequence = 747
    let [<Literal>] ConstructRequiresComputationExpression = 748
    let [<Literal>] EmptyRecordInvalid = 789
    let [<Literal>] UseBindingsIllegalInModules = 524
    let [<Literal>] LocalClassBindingsCannotBeInline = 894
    let [<Literal>] UnusedValue = 1182
    let [<Literal>] UnusedThisVariable = 1183

    let [<Literal>] MissingErrorNumber = 193

    let [<Literal>] undefinedIndexerMessageSuffix = " does not define the field, constructor or member 'Item'."
    let [<Literal>] ifExprMissingElseBranch = "This 'if' expression is missing an 'else' branch."
    let [<Literal>] expressionIsAFunctionMessage = "This expression is a function value, i.e. is missing arguments. Its type is "

[<AbstractClass>]
type FcsErrorsStageProcessBase(fsFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let document = daemonProcess.Document
    let nodeSelectionProvider = FSharpTreeNodeSelectionProvider.Instance

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
        match nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | expr -> highlightingCtor expr :> _

    let createHighlightingFromParentNode highlightingCtor range: IHighlighting =
        match nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | node ->

        match node.GetContainingNode() with
        | null -> null
        | parent -> highlightingCtor parent :> _

    let createHighlightingFromNodeWithMessage highlightingCtor range (error: FSharpErrorInfo): IHighlighting =
        let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
        if isNotNull expr then highlightingCtor (expr, error.Message) :> _ else
        null

    let createHighlightingFromParentNodeWithMessage highlightingCtor range (error: FSharpErrorInfo): IHighlighting =
        match nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | node ->

        match node.GetContainingNode() with
        | null -> null
        | parent -> highlightingCtor (parent, error.Message) :> _

    let createHighlighting (error: FSharpErrorInfo) (range: DocumentRange): IHighlighting =
        match error.ErrorNumber with
        | TypeEquation when error.Message.StartsWith(ifExprMissingElseBranch, StringComparison.Ordinal) ->
            createHighlightingFromNodeWithMessage UnitTypeExpectedError range error

        | NotAFunction ->
            let notAFunctionNode = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
            match tryFindRootPrefixAppWhereExpressionIsFunc notAFunctionNode with
            | :? IPrefixAppExpr as prefixAppExpr ->
                NotAFunctionError(notAFunctionNode.IgnoreParentParens(), prefixAppExpr) :> _
            | _ -> createGenericHighlighting error range

        | FieldNotMutable ->
            createHighlightingFromNode FieldOrValueNotMutableError range

        | VarBoundTwice -> 
            createHighlightingFromNode VarBoundTwiceError range

        | UndefinedName ->
            if (endsWith undefinedIndexerMessageSuffix error.Message &&
                    let indexer = fsFile.GetNode<IItemIndexerExpr>(range) in isNotNull indexer) then
                UndefinedIndexerError(fsFile.GetNode(range)) :> _ else

            let identifier = fsFile.GetNode(range)
            let referenceOwner = FSharpReferenceOwnerNavigator.GetByIdentifier(identifier)
            if isNotNull referenceOwner then UndefinedNameError(referenceOwner.Reference, error.Message) :> _ else

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

        | MatchIncomplete ->
            createHighlightingFromParentNodeWithMessage MatchIncompleteWarning range error

        | EnumMatchIncomplete ->
            createHighlightingFromParentNodeWithMessage EnumMatchIncompleteWarning range error

        | ValNotMutable ->
            let setExpr = fsFile.GetNode<ISetExpr>(range)
            if isNull setExpr then createGenericHighlighting error range else

            let refExpr = setExpr.LeftExpression.As<IReferenceExpr>()
            if isNull refExpr then createGenericHighlighting error range else

            FieldOrValueNotMutableError(refExpr) :> _

        | UnitTypeExpected ->
            let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
            let expr = getUnusedExpr expr
            UnitTypeExpectedWarning(expr, error.Message) :> _

        | UseBindingsIllegalInModules ->
            createHighlightingFromNode UseBindingsIllegalInModulesWarning range

        | NoImplementationGiven
        | NoImplementationGivenWithSuggestion ->
            let node = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
            match node.Parent with
            | :? IFSharpTypeDeclaration as typeDecl when typeDecl.Identifier == node ->
                NoImplementationGivenTypeError(typeDecl, error.Message) :> _

            | :? IInterfaceImplementation as impl when impl.TypeName == node ->
                NoImplementationGivenInterfaceError(impl, error.Message) :> _

            | :? ITypeReferenceName as typeName when
                    isNotNull (InterfaceImplementationNavigator.GetByTypeName(typeName)) ->
                let impl = InterfaceImplementationNavigator.GetByTypeName(typeName)
                NoImplementationGivenInterfaceError(impl, error.Message) :> _

            | _ -> createGenericHighlighting error range

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

        | ConstructRequiresListArrayOrSequence ->
            createHighlightingFromNode YieldRequiresSeqExpressionError range

        | ConstructRequiresComputationExpression ->
            createHighlightingFromNode ReturnRequiresComputationExpressionError range

        | EmptyRecordInvalid ->
            createHighlightingFromNodeWithMessage EmptyRecordInvalidError range error

        | MissingErrorNumber when startsWith expressionIsAFunctionMessage error.Message ->
            let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
            let expr = getUnusedExpr expr
            FunctionValueUnexpectedWarning(expr, error.Message) :> _

        | NamespaceCannotContainValues ->
            createHighlightingFromNode NamespaceCannotContainValuesError range

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
                    | :? IHighlightingWithSecondaryRanges as highlighting ->
                        for range in highlighting.CalculateSecondaryRanges() do
                            highlightings.Add(HighlightingInfo(range, highlighting))
                        highlighting :> _   
                    | highlighting -> highlighting

                highlightings.Add(HighlightingInfo(highlighting.CalculateRange(), highlighting))
            x.SeldomInterruptChecker.CheckForInterrupt()

        committer.Invoke(DaemonStageResult(highlightings))
