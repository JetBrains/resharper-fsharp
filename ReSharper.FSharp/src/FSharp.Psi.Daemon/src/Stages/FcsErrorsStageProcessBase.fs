namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open System
open FSharp.Compiler.Diagnostics
open JetBrains.Application
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

type IIgnoredHighlighting =
    inherit IHighlighting

type IgnoredHighlighting() =
    static member val Instance = IgnoredHighlighting()

    interface IIgnoredHighlighting with
        member this.CalculateRange() = DocumentRange.InvalidRange
        member this.ErrorStripeToolTip = ""
        member this.IsValid() = true
        member this.ToolTip = ""


[<AutoOpen>]
module FSharpErrors =
    // Error numbers as reported by FCS:
    // * Exception types: fsharp/CompilerDiagnostics.fs#L217
    // * Generated from text: fsharp/FSComp.txt

    let [<Literal>] TypeEquation = 1
    let [<Literal>] NotAFunction = 3
    let [<Literal>] FieldNotMutable = 5
    let [<Literal>] RuntimeCoercionSourceSealed = 16
    let [<Literal>] UnitTypeExpected = 20
    let [<Literal>] MatchIncomplete = 25
    let [<Literal>] RuleNeverMatched = 26
    let [<Literal>] ValNotMutable = 27
    let [<Literal>] VarBoundTwice = 38
    let [<Literal>] UndefinedName = 39
    let [<Literal>] ErrorFromAddingConstraint = 43
    let [<Literal>] UpcastUnnecessary = 66
    let [<Literal>] TypeTestUnnecessary = 67
    let [<Literal>] IndeterminateType = 72
    let [<Literal>] EnumMatchIncomplete = 104
    let [<Literal>] NamespaceCannotContainValues = 201
    let [<Literal>] MissingErrorNumber = 193
    let [<Literal>] ModuleOrNamespaceRequired = 222
    let [<Literal>] UnrecognizedOption = 243
    let [<Literal>] NoImplementationGiven = 365
    let [<Literal>] NoImplementationGivenWithSuggestion = 366
    let [<Literal>] MemberIsNotAccessible = 491
    let [<Literal>] MethodIsNotAnInstanceMethod = 493
    let [<Literal>] UseBindingsIllegalInImplicitClassConstructors = 523
    let [<Literal>] UseBindingsIllegalInModules = 524
    let [<Literal>] OnlyClassCanTakeValueArguments = 552
    let [<Literal>] LetAndForNonRecBindings = 576
    let [<Literal>] ExpectedExpressionAfterLet = 588
    let [<Literal>] SuccessiveArgsShouldBeSpacedOrTupled = 597
    let [<Literal>] StaticFieldUsedWhenInstanceFieldExpected = 627
    let [<Literal>] InstanceMemberRequiresTarget = 673
    let [<Literal>] UnionCaseExpectsTupledArguments = 727
    let [<Literal>] ConstructRequiresListArrayOrSequence = 747
    let [<Literal>] ConstructRequiresComputationExpression = 748
    let [<Literal>] ObjectOfIndeterminateTypeUsedRequireTypeConstraint = 752
    let [<Literal>] FieldRequiresAssignment = 764
    let [<Literal>] EmptyRecordInvalid = 789
    let [<Literal>] PropertyIsStatic = 809
    let [<Literal>] LocalClassBindingsCannotBeInline = 894
    let [<Literal>] TypeAbbreviationsCannotHaveAugmentations = 964
    let [<Literal>] UnusedValue = 1182
    let [<Literal>] UnusedThisVariable = 1183
    let [<Literal>] CantTakeAddressOfExpression = 3236
    let [<Literal>] SingleQuoteInSingleQuote = 3373

    let [<Literal>] undefinedIndexerMessageSuffix = " does not define the field, constructor or member 'Item'."
    let [<Literal>] undefinedGetSliceMessageSuffix = " does not define the field, constructor or member 'GetSlice'."
    let [<Literal>] ifExprMissingElseBranch = "This 'if' expression is missing an 'else' branch."
    let [<Literal>] expressionIsAFunctionMessage = "This expression is a function value, i.e. is missing arguments. Its type is "
    let [<Literal>] typeConstraintMismatchMessage = "Type constraint mismatch. The type \n    '(.+)'    \nis not compatible with type\n    '(.+)'"

    let [<Literal>] typeEquationMessage = "This expression was expected to have type\n    '(.+)'    \nbut here has type\n    '(.+)'"
    let [<Literal>] typeDoesNotMatchMessage = "The type '(.+)' does not match the type '(.+)'"
    let [<Literal>] elseBranchHasWrongTypeMessage = "All branches of an 'if' expression must return values implicitly convertible to the type of the first branch, which here is '(.+)'. This branch returns a value of type '(.+)'."
    let [<Literal>] ifBranchSatisfyContextTypeRequirements = "The 'if' expression needs to have type '(.+)' to satisfy context type requirements\. It currently has type '(.+)'"
    let [<Literal>] typeMisMatchTupleLengths = "Type mismatch. Expecting a\n    '(.+)'    \nbut given a\n    '(.+)'    \nThe tuples have differing lengths of \\d+ and \\d+"

    let isDirectiveSyntaxError number =
        number >= 232 && number <= 235

[<AbstractClass>]
type FcsErrorsStageProcessBase(fsFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let document = daemonProcess.Document
    let nodeSelectionProvider = FSharpTreeNodeSelectionProvider.Instance

    let getDocumentRange (error: FSharpDiagnostic) =
        if error.StartLine = 0 || error.ErrorNumber = ModuleOrNamespaceRequired then
            DocumentRange(document, TextRange(0, document.GetLineEndOffsetWithLineBreak(Line.O)))
        else
            let startOffset = getDocumentOffset document (docCoords error.StartLine error.StartColumn)
            let endOffset = getDocumentOffset document (docCoords error.EndLine error.EndColumn)
            DocumentRange(document, TextRange(startOffset, endOffset))

    let createGenericHighlighting (error: FSharpDiagnostic) range: IHighlighting =
        match error.Severity with
        | FSharpDiagnosticSeverity.Info -> InfoHighlighting(error.Message, range) :> _
        | FSharpDiagnosticSeverity.Warning -> WarningHighlighting(error.Message, range) :> _
        | _ -> ErrorHighlighting(error.Message, range) :> _

    /// Finds node of the corresponding type in the range.
    let createHighlightingFromNode highlightingCtor range: IHighlighting =
        match nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | expr -> highlightingCtor expr :> _

    /// Finds node in the range and creates highlighting for the smallest containing node of the corresponding type.
    let createHighlightingFromParentNode highlightingCtor range: IHighlighting =
        match nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | node ->

        match node.GetContainingNode() with
        | null -> null
        | parent -> highlightingCtor parent :> _

    /// Finds node in the range and creates highlighting for the smallest containing node of the corresponding type.
    let createHighlightingFromGrandparentNode highlightingCtor range: IHighlighting =
        match nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | node ->

        match node.GetContainingNode() with
        | null -> null
        | parent ->

        match parent.GetContainingNode() with
        | null -> null
        | grandparent -> highlightingCtor grandparent :> _

    let createHighlightingFromNodeWithMessage highlightingCtor range (error: FSharpDiagnostic): IHighlighting =
        let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
        if isNotNull expr then highlightingCtor (expr, error.Message) :> _ else
        null

    let createHighlightingFromParentNodeWithMessage highlightingCtor range (error: FSharpDiagnostic): IHighlighting =
        match nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | node ->

        match node.GetContainingNode() with
        | null -> null
        | parent -> highlightingCtor (parent, error.Message) :> _

    /// Finds the smallest node of the corresponding type at offset.
    let createHighlightingFromNodeAtOffset highlightingCtor offset: IHighlighting =
        match fsFile.FindTokenAt(TreeOffset(offset)) with
        | null -> null
        | token ->

        match token.GetContainingNode() with
        | null -> null
        | node -> highlightingCtor node :> _

    let createHighlighting (error: FSharpDiagnostic) (range: DocumentRange): IHighlighting =
        match error.ErrorNumber with
        | TypeEquation ->
            match error.Message with
            | message when message.StartsWith(ifExprMissingElseBranch, StringComparison.Ordinal) ->
                createHighlightingFromNodeWithMessage UnitTypeExpectedError range error

            | Regex typeEquationMessage [expectedType; actualType]
            | Regex elseBranchHasWrongTypeMessage [expectedType; actualType] ->
                let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
                let expr = getResultExpr expr
                if isNotNull expr then
                    match expectedType with
                    | "unit" -> createHighlightingFromNodeWithMessage UnitTypeExpectedError range error
                    | _ -> TypeEquationError(expectedType, actualType, expr, error.Message) :> _
                else null

            | Regex typeDoesNotMatchMessage [expectedType; actualType] ->
                let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
                TypeDoesNotMatchTypeError(expectedType, actualType, expr, error.Message)

            | Regex ifBranchSatisfyContextTypeRequirements [expectedType; actualType] ->
                let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
                IfExpressionNeedsTypeToSatisfyTypeRequirementsError(expectedType, actualType, expr, error.Message)

            | Regex typeMisMatchTupleLengths [expectedType; actualType] ->
                let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
                TypeMisMatchTuplesHaveDifferingLengthsError(expectedType, actualType, expr, error.Message)
            | _ -> createGenericHighlighting error range

        | NotAFunction ->
            let notAFunctionNode = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
            match getOutermostPrefixAppExpr notAFunctionNode with
            | :? IPrefixAppExpr as prefixAppExpr ->
                NotAFunctionError(notAFunctionNode.IgnoreParentParens(), prefixAppExpr) :> _
            | _ -> createGenericHighlighting error range

        | FieldNotMutable ->
            createHighlightingFromNode FieldNotMutableError range

        | RuntimeCoercionSourceSealed ->
            match fsFile.GetNode<IFSharpPattern>(range) with
            | null -> createHighlightingFromNodeWithMessage RuntimeCoercionSourceSealedError range error
            | _ -> createGenericHighlighting error range

        | VarBoundTwice ->
            createHighlightingFromNode VarBoundTwiceError range

        | UndefinedName ->
            let message = error.Message
            let isUndefinedIndexerText =
                endsWith undefinedIndexerMessageSuffix message || endsWith undefinedGetSliceMessageSuffix message

            let tryGetIndexerExpr (range: DocumentRange): ITreeNode option =
                let indexerExpr = fsFile.GetNode<IItemIndexerExpr>(range)
                if isNotNull indexerExpr && isNotNull indexerExpr.IndexerArgList then
                    Some(indexerExpr.IndexerArgList) else

                let appExpr = (fsFile.GetNode<IPrefixAppExpr>(range))
                if isNotNull appExpr && appExpr.ArgumentExpression :? IListExpr then
                    Some(appExpr.ArgumentExpression) else

                None

            match isUndefinedIndexerText, tryGetIndexerExpr range with
            | true, Some(treeNode) -> UndefinedIndexerError(treeNode) :> _
            | _ ->

            let identifier = fsFile.GetNode(range)
            let referenceOwner = FSharpReferenceOwnerNavigator.GetByIdentifier(identifier)
            if isNotNull referenceOwner then UndefinedNameError(referenceOwner.Reference, message) :> _ else

            UnresolvedHighlighting(message, range) :> _

        | ErrorFromAddingConstraint ->
            createHighlightingFromNodeWithMessage AddingConstraintError range error

        | UpcastUnnecessary ->
            createHighlightingFromNode UpcastUnnecessaryWarning range

        | TypeTestUnnecessary ->
            createHighlightingFromNodeWithMessage TypeTestUnnecessaryWarning range error

        | IndeterminateType ->
            createHighlightingFromNode IndeterminateTypeError range

        | UnusedValue ->
            match fsFile.GetNode<IReferencePat>(range) with
            | null -> UnusedHighlighting(error.Message, range) :> _
            | refPat ->

            let pat = FSharpPatternUtil.ignoreParentAsPatsFromRight refPat
            let binding = TopBindingNavigator.GetByHeadPattern(pat)
            let decl = LetBindingsDeclarationNavigator.GetByBinding(binding)
            if isNotNull decl && binding.HasParameters && not (Seq.isEmpty binding.AttributesEnumerable) then
                IgnoredHighlighting.Instance :> _
            else
                UnusedValueWarning(refPat) :> _

        | RuleNeverMatched ->
            let matchClause = fsFile.GetNode<IMatchClause>(range)
            if isNull matchClause then createGenericHighlighting error range else
            RuleNeverMatchedWarning(matchClause) :> _

        | MatchIncomplete ->
            let fsPattern = fsFile.GetNode<IFSharpPattern>(range)
            if isNotNull fsPattern then createGenericHighlighting error range else

            let matchLambdaExpr = fsFile.GetNode<IMatchLambdaExpr>(range)
            if isNotNull matchLambdaExpr then createGenericHighlighting error range else

            createHighlightingFromParentNodeWithMessage MatchIncompleteWarning range error

        | EnumMatchIncomplete ->
            createHighlightingFromParentNodeWithMessage EnumMatchIncompleteWarning range error

        | ValNotMutable ->
            let setExpr = fsFile.GetNode<ISetExpr>(range)
            if isNull setExpr then createGenericHighlighting error range else

            let refExpr = setExpr.LeftExpression.As<IReferenceExpr>()
            if isNull refExpr then createGenericHighlighting error range else

            ValueNotMutableError(refExpr) :> _

        | UnitTypeExpected ->
            let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
            let expr = getResultExpr expr
            UnitTypeExpectedWarning(expr, error.Message) :> _

        | UseBindingsIllegalInModules ->
            createHighlightingFromNode UseBindingsIllegalInModulesWarning range

        | OnlyClassCanTakeValueArguments ->
            match fsFile.GetNode<IFSharpTypeDeclaration>(range) with
            | null -> createGenericHighlighting error range
            | typeDecl ->

            match typeDecl.PrimaryConstructorDeclaration with
            | null -> createGenericHighlighting error range
            | ctorDecl -> OnlyClassCanTakeValueArgumentsError(ctorDecl) :> _

        | NoImplementationGiven ->
            let node = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
            match node.Parent with
            | :? IFSharpTypeDeclaration as typeDecl when typeDecl.Identifier == node ->
                NoImplementationGivenInTypeError(typeDecl, error.Message) :> _

            | :? IInterfaceImplementation as impl when impl.TypeName == node ->
                NoImplementationGivenInInterfaceError(impl, error.Message) :> _

            | :? ITypeReferenceName as typeName when
                    isNotNull (InterfaceImplementationNavigator.GetByTypeName(typeName)) ->
                let impl = InterfaceImplementationNavigator.GetByTypeName(typeName)
                NoImplementationGivenInInterfaceError(impl, error.Message) :> _

            | _ -> createGenericHighlighting error range

        | NoImplementationGivenWithSuggestion ->
            let node = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
            match node.Parent with
            | :? IFSharpTypeDeclaration as typeDecl when typeDecl.Identifier == node ->
                NoImplementationGivenInTypeWithSuggestionError(typeDecl, error.Message) :> _

            | :? IInterfaceImplementation as impl when impl.TypeName == node ->
                NoImplementationGivenInInterfaceWithSuggestionError(impl, error.Message) :> _

            | :? ITypeReferenceName as typeName when
                    isNotNull (InterfaceImplementationNavigator.GetByTypeName(typeName)) ->
                let impl = InterfaceImplementationNavigator.GetByTypeName(typeName)
                NoImplementationGivenInInterfaceWithSuggestionError(impl, error.Message) :> _

            | _ -> createGenericHighlighting error range

        | MemberIsNotAccessible ->
            createHighlightingFromNode MemberIsNotAccessibleError range

        | MethodIsNotAnInstanceMethod ->
            let refExpr = nodeSelectionProvider.GetExpressionInRange<IReferenceExpr>(fsFile, range, false, null)
            if isNotNull refExpr then MethodIsStaticError(refExpr) :> _ else

            let appExpr = nodeSelectionProvider.GetExpressionInRange<IPrefixAppExpr>(fsFile, range, false, null)
            if isNull appExpr then null else

            let appRefExpr = appExpr.FunctionExpression.As<IReferenceExpr>()
            if isNotNull appRefExpr then MethodIsStaticError(appRefExpr) :> _ else

            null

        | UseBindingsIllegalInImplicitClassConstructors ->
            createHighlightingFromNode UseKeywordIllegalInPrimaryCtorError range

        | PropertyIsStatic ->
            createHighlightingFromNode PropertyIsStaticError range

        | LocalClassBindingsCannotBeInline ->
            createHighlightingFromParentNode LocalClassBindingsCannotBeInlineError range

        | TypeAbbreviationsCannotHaveAugmentations ->
            // For `type Foo.Bar<'T> with ...` FCS reports `Foo.Bar` lid range, we're interested in `Bar` offset.
            createHighlightingFromNodeAtOffset TypeAbbreviationsCannotHaveAugmentationsError range.EndOffset.Offset

        | LetAndForNonRecBindings ->
            createHighlightingFromGrandparentNode LetAndForNonRecBindingsError range

        | UnusedThisVariable ->
            createHighlightingFromParentNode UnusedThisVariableWarning range

        | CantTakeAddressOfExpression ->
            createHighlightingFromNode CantTakeAddressOfExpressionError range

        | SingleQuoteInSingleQuote ->
            createHighlightingFromNodeWithMessage SingleQuoteInSingleQuoteError range error

        | ObjectOfIndeterminateTypeUsedRequireTypeConstraint ->
            createHighlightingFromNode IndexerIndeterminateTypeError range

        | FieldRequiresAssignment ->
            createHighlightingFromNodeWithMessage FieldRequiresAssignmentError range error

        | ExpectedExpressionAfterLet ->
            createHighlightingFromParentNode ExpectedExpressionAfterLetError range

        | SuccessiveArgsShouldBeSpacedOrTupled ->
            createHighlightingFromNode SuccessiveArgsShouldBeSpacedOrTupledError range

        | StaticFieldUsedWhenInstanceFieldExpected ->
            createHighlightingFromNode FieldIsStaticError range

        | InstanceMemberRequiresTarget ->
            match fsFile.GetNode<IMemberDeclaration>(range) with
            | null -> null
            | memberDecl -> InstanceMemberRequiresTargetError(memberDecl) :> _

        | UnionCaseExpectsTupledArguments ->
            createHighlightingFromNodeWithMessage UnionCaseExpectsTupledArgumentsError range error

        | ConstructRequiresListArrayOrSequence ->
            createHighlightingFromNode YieldRequiresSeqExpressionError range

        | ConstructRequiresComputationExpression ->
            createHighlightingFromNode ReturnRequiresComputationExpressionError range

        | EmptyRecordInvalid ->
            createHighlightingFromNodeWithMessage EmptyRecordInvalidError range error

        | MissingErrorNumber ->
            match error.Message with
            | x when startsWith expressionIsAFunctionMessage x ->
                let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
                let expr = getResultExpr expr
                FunctionValueUnexpectedWarning(expr, error.Message) :> _

            | Regex typeConstraintMismatchMessage [mismatchedType; typeConstraint] ->
                let highlighting =
                    match typeConstraint with
                    | "unit" ->
                        let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
                        let expr = getResultExpr expr
                        if isNull expr then null else UnitTypeExpectedError(expr, error.Message)
                    | _ -> null

                if isNotNull highlighting then highlighting :> _ else

                let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
                TypeConstraintMismatchError(mismatchedType, expr, error.Message)

            | _ -> null

        | NamespaceCannotContainValues ->
            let binding = fsFile.GetNode<IBindingLikeDeclaration>(range)
            if isNotNull binding then NamespaceCannotContainBindingsError(binding) :> _ else

            let expr = fsFile.GetNode<IDoLikeStatement>(range)
            if isNotNull expr then NamespaceCannotContainExpressionsError(expr) :> _ else null

        | _ -> createGenericHighlighting error range

    abstract ShouldAddDiagnostic: error: FSharpDiagnostic * range: DocumentRange -> bool
    default x.ShouldAddDiagnostic(error: FSharpDiagnostic, _) =
        error.ErrorNumber <> UnrecognizedOption

    member x.Execute(errors: FSharpDiagnostic[], committer: Action<DaemonStageResult>) =
        let daemonProcess = x.DaemonProcess
        let sourceFile = daemonProcess.SourceFile
        let consumer = FilteringHighlightingConsumer(sourceFile, fsFile, daemonProcess.ContextBoundSettingsStore)

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

                if highlighting != IgnoredHighlighting.Instance then
                    consumer.ConsumeHighlighting(HighlightingInfo(highlighting.CalculateRange(), highlighting))

            Interruption.Current.CheckAndThrow()

        committer.Invoke(DaemonStageResult(consumer.Highlightings))
