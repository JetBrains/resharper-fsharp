namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open System
open System.Collections.Generic
open FSharp.Compiler.Diagnostics
open FSharp.Compiler.Diagnostics.ExtendedData
open FSharp.Compiler.Symbols
open JetBrains.Application
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

#nowarn "57"

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
    let [<Literal>] IndeterminateRuntimeCoercion = 8
    let [<Literal>] RuntimeCoercionSourceSealed = 16
    let [<Literal>] UnitTypeExpected = 20
    let [<Literal>] MatchIncomplete = 25
    let [<Literal>] RuleNeverMatched = 26
    let [<Literal>] ValNotMutable = 27
    let [<Literal>] ValueNotContainedMutability = 34
    let [<Literal>] VarBoundTwice = 38
    let [<Literal>] UndefinedName = 39
    let [<Literal>] ErrorFromAddingConstraint = 43
    let [<Literal>] UpperCaseIdentifierInPattern = 49
    let [<Literal>] UpcastUnnecessary = 66
    let [<Literal>] TypeTestUnnecessary = 67
    let [<Literal>] IndeterminateType = 72
    let [<Literal>] EnumMatchIncomplete = 104
    let [<Literal>] NamespaceCannotContainValues = 201
    let [<Literal>] MissingErrorNumber = 193
    let [<Literal>] ModuleOrNamespaceRequired = 222
    let [<Literal>] UnrecognizedOption = 243
    let [<Literal>] DefinitionsInSigAndImplNotCompatibleFieldWasPresent = 311
    let [<Literal>] DefinitionsInSigAndImplNotCompatibleFieldOrderDiffer = 312
    let [<Literal>] DefinitionsInSigAndImplNotCompatibleFieldRequiredButNotSpecified = 313
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
    let [<Literal>] UnionCaseDoesNotTakeArguments = 725
    let [<Literal>] UnionCaseExpectsTupledArguments = 727
    let [<Literal>] ConstructRequiresListArrayOrSequence = 747
    let [<Literal>] ConstructRequiresComputationExpression = 748
    let [<Literal>] ObjectOfIndeterminateTypeUsedRequireTypeConstraint = 752
    let [<Literal>] AbstractTypeCannotBeInstantiated = 759
    let [<Literal>] FieldRequiresAssignment = 764
    let [<Literal>] EmptyRecordInvalid = 789
    let [<Literal>] InvalidUseOfTypeName = 800
    let [<Literal>] PropertyIsStatic = 809
    let [<Literal>] PropertyCannotBeSet = 810
    let [<Literal>] AttributeIsNotValidOnThisElement = 842
    let [<Literal>] LocalClassBindingsCannotBeInline = 894
    let [<Literal>] TypeAbbreviationsCannotHaveAugmentations = 964
    let [<Literal>] UnusedValue = 1182
    let [<Literal>] UnusedThisVariable = 1183
    let [<Literal>] LiteralPatternDoesNotTakeArguments = 3191
    let [<Literal>] ArgumentNamesInSignatureAndImplementationDoNotMatch = 3218
    let [<Literal>] CantTakeAddressOfExpression = 3236
    let [<Literal>] SingleQuoteInSingleQuote = 3373
    let [<Literal>] XmlDocSignatureCheckFailed = 3390
    let [<Literal>] InvalidXmlDocPosition = 3520

    let isDirectiveSyntaxError number =
        number >= 232 && number <= 235

[<AbstractClass>]
type FcsErrorsStageProcessBase(fsFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let document = daemonProcess.Document
    let nodeSelectionProvider = FSharpTreeNodeSelectionProvider.Instance
    let cachedFcsDiagnostics = Dictionary()

    let getDocumentRange (error: FSharpDiagnostic) =
        if error.StartLine = 0 || error.ErrorNumber = ModuleOrNamespaceRequired then
            let startOffset = document.GetDocumentStartOffset()
            let endOffset = document.GetLineEndDocumentOffsetWithLineBreak(Line.O)
            DocumentRange(&startOffset, &endOffset)
        else
            let startOffset = getDocumentOffset document (docCoords error.StartLine error.StartColumn)
            let endOffset = getDocumentOffset document (docCoords error.EndLine error.EndColumn)
            DocumentRange(&startOffset, &endOffset)

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

    let createHighlightingFromMappedExpression mapping highlightingCtor range (error: FSharpDiagnostic): IHighlighting =
        let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) |> mapping
        if isNotNull expr then highlightingCtor(expr, error.Message) :> _ else null

    let createCachedDiagnostic error range =
        let diagnosticInfo = FcsCachedDiagnosticInfo(error, fsFile, range)
        cachedFcsDiagnostics[diagnosticInfo.Offset] <- error
        diagnosticInfo

    let createTypeMismatchHighlighting highlightingCtor range error : IHighlighting =
        match nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) with
        | null -> null
        | expr ->
            let diagnosticInfo = createCachedDiagnostic error range
            highlightingCtor (diagnosticInfo, expr, error.Message) :> _

    let createHighlighting (error: FSharpDiagnostic) (range: DocumentRange): IHighlighting =
        match error.ErrorNumber with
        | TypeEquation ->
            match error.ExtendedData with
            | Some(:? TypeMismatchDiagnosticExtendedData as data) when
                    data.ContextInfo = DiagnosticContextInfo.OmittedElseBranch ->
                createHighlightingFromNodeWithMessage UnitTypeExpectedError range error

            | Some(:? TypeMismatchDiagnosticExtendedData as data) when
                    // TODO: currently FollowingPatternMatchClause context info can be returned
                    // even if the expression is not returned from the end of the branch
                    data.ContextInfo = DiagnosticContextInfo.FollowingPatternMatchClause ->
                createTypeMismatchHighlighting MatchClauseWrongTypeError range error

            | Some(:? TypeMismatchDiagnosticExtendedData as data) ->
                let expectedType = data.ExpectedType
                let actualType = data.ActualType

                if expectedType.IsTupleType && actualType.IsTupleType &&
                   expectedType.GenericArguments.Count <> actualType.GenericArguments.Count then
                    createTypeMismatchHighlighting TypeMisMatchTuplesHaveDifferingLengthsError range error
                else
                    let expr = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
                    let expr = getResultExpr expr

                    if isNull expr then
                        null
                    elif isUnit expectedType then
                        createHighlightingFromNodeWithMessage UnitTypeExpectedError range error
                    else
                        createTypeMismatchHighlighting TypeEquationError range error

            | _ -> createGenericHighlighting error range

        | NotAFunction ->
            let notAFunctionNode = nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null)
            match getOutermostPrefixAppExpr notAFunctionNode with
            | :? IPrefixAppExpr as prefixAppExpr ->
                NotAFunctionError(notAFunctionNode.IgnoreParentParens(), prefixAppExpr) :> _
            | _ -> createGenericHighlighting error range

        | FieldNotMutable ->
            createHighlightingFromNode FieldNotMutableError range

        | IndeterminateRuntimeCoercion ->
            let isInstPat = nodeSelectionProvider.GetExpressionInRange<IIsInstPat>(fsFile, range, false, null)
            if isNotNull isInstPat then
                IndeterminateTypeRuntimeCoercionPatternError(isInstPat, error.Message) else

            let typeTestExpr = nodeSelectionProvider.GetExpressionInRange<ITypeTestExpr>(fsFile, range, false, null)
            if isNotNull typeTestExpr then
                IndeterminateTypeRuntimeCoercionExpressionError(typeTestExpr, error.Message) else

            createGenericHighlighting error range

        | RuntimeCoercionSourceSealed ->
            match fsFile.GetNode<IFSharpPattern>(range) with
            | null -> createHighlightingFromNodeWithMessage RuntimeCoercionSourceSealedError range error
            | _ -> createGenericHighlighting error range

        | VarBoundTwice ->
            createHighlightingFromNode VarBoundTwiceError range

        | UndefinedName ->
            match nodeSelectionProvider.GetExpressionInRange<IFSharpExpression>(fsFile, range, false, null) with
            | :? IPrefixAppExpr as prefixAppExpr when prefixAppExpr.IsIndexerLike ->
                UndefinedIndexerLikeExprError(prefixAppExpr, error.Message) :> _

            | :? IItemIndexerExpr as indexerExpr ->
                UndefinedIndexerError(indexerExpr, error.Message)

            | _ ->

            let identifier = fsFile.GetNode(range)
            let referenceOwner = FSharpReferenceOwnerNavigator.GetByIdentifier(identifier)
            if isNotNull referenceOwner then UndefinedNameError(referenceOwner.Reference, error.Message) :> _ else

            UnresolvedHighlighting(error.Message, range) :> _

        | ErrorFromAddingConstraint ->
            createHighlightingFromNodeWithMessage AddingConstraintError range error

        | UpperCaseIdentifierInPattern ->
            let identifier = fsFile.GetNode(range)
            let referenceOwner = FSharpReferenceOwnerNavigator.GetByIdentifier(identifier)
            if isNull referenceOwner then null else

            UpperCaseIdentifierInPatternWarning(referenceOwner.Reference, error.Message) :> _

        | UpcastUnnecessary ->
            createHighlightingFromNode UpcastUnnecessaryWarning range

        | TypeTestUnnecessary ->
            createHighlightingFromNodeWithMessage TypeTestUnnecessaryWarning range error

        | IndeterminateType ->
            createHighlightingFromNode IndeterminateTypeError range

        | UnusedValue ->
            match fsFile.GetNode<IReferencePat>(range) with
            | null ->
                match fsFile.GetNode<IReferenceExpr>(range) with
                | null ->
                    UnusedHighlighting(error.Message, range) :> _

                | refExpr ->
                    let tryGetSymbol (refExpr: IReferenceExpr) =
                        let reference = refExpr.Reference
                        let offset = reference.SymbolOffset
                        if not (offset.IsValid()) then Unchecked.defaultof<_> else

                        let fcsSymbolUse = refExpr.FSharpFile.GetSymbolDeclaration(offset.Offset)
                        if isNull fcsSymbolUse then Unchecked.defaultof<_> else

                        fcsSymbolUse.Symbol

                    match tryGetSymbol refExpr with
                    | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsReferencedValue ->
                        IgnoredHighlighting.Instance :> _
                    | _ ->
                        UnusedHighlighting(error.Message, range) :> _

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

            createHighlightingFromParentNodeWithMessage MatchIncompleteWarning range error

        | EnumMatchIncomplete ->
            createHighlightingFromParentNodeWithMessage EnumMatchIncompleteWarning range error

        | ValNotMutable ->
            let setExpr = fsFile.GetNode<ISetExpr>(range)
            if isNull setExpr then createGenericHighlighting error range else

            let refExpr = setExpr.LeftExpression.As<IReferenceExpr>()
            if isNull refExpr then createGenericHighlighting error range else

            ValueNotMutableError(refExpr) :> _

        | ValueNotContainedMutability ->
            match error.ExtendedData with
            | Some (:? ValueNotContainedDiagnosticExtendedData) ->
                createHighlightingFromNodeWithMessage ValueNotContainedMutabilityAttributesDifferError range error
            | _ -> createGenericHighlighting error range
        
        | UnitTypeExpected ->
            createHighlightingFromMappedExpression getResultExpr UnitTypeExpectedWarning range error

        | UseBindingsIllegalInModules ->
            createHighlightingFromNode UseBindingsIllegalInModulesWarning range

        | OnlyClassCanTakeValueArguments ->
            match fsFile.GetNode<IFSharpTypeDeclaration>(range) with
            | null -> createGenericHighlighting error range
            | typeDecl ->

            match typeDecl.PrimaryConstructorDeclaration with
            | null -> createGenericHighlighting error range
            | ctorDecl -> OnlyClassCanTakeValueArgumentsError(ctorDecl) :> _

        | DefinitionsInSigAndImplNotCompatibleFieldWasPresent ->
            createHighlightingFromParentNodeWithMessage DefinitionsInSigAndImplNotCompatibleFieldWasPresentError range error

        | DefinitionsInSigAndImplNotCompatibleFieldOrderDiffer ->
            createHighlightingFromParentNodeWithMessage DefinitionsInSigAndImplNotCompatibleFieldOrderDifferError range error

        | DefinitionsInSigAndImplNotCompatibleFieldRequiredButNotSpecified ->
            createHighlightingFromParentNodeWithMessage
                DefinitionsInSigAndImplNotCompatibleFieldRequiredButNotSpecifiedError
                range
                error
        
        | NoImplementationGiven ->
            let node = nodeSelectionProvider.GetExpressionInRange<ITreeNode>(fsFile, range, false, null)
            match node.Parent with
            | :? IFSharpTypeDeclaration as typeDecl when typeDecl.Identifier == node ->
                NoImplementationGivenInTypeError(typeDecl, error.Message) :> _

            | :? IInterfaceImplementation as impl when impl.TypeName == node ->
                NoImplementationGivenInInterfaceError(impl, error.Message) :> _

            | :? ITypeReferenceName as typeName when
                    isNotNull (InterfaceImplementationNavigator.GetByTypeName(typeName)) ->
                let impl = InterfaceImplementationNavigator.GetByTypeName(typeName)
                NoImplementationGivenInInterfaceError(impl, error.Message) :> _

            | _ ->
                
            match node with
            | :? IObjExpr as objExpr -> NoImplementationGivenInTypeWithSuggestionError(objExpr, error.Message) :> _
            | _ -> createGenericHighlighting error range

        | NoImplementationGivenWithSuggestion ->
            let token = fsFile.FindTokenAt(range.StartOffset + 1)
            let impl = (getParent token).As<IInterfaceImplementation>()
            if getTokenType token == FSharpTokenType.INTERFACE &&
                    isNotNull (ObjExprNavigator.GetByInterfaceImplementation(impl)) then
                NoImplementationGivenInInterfaceWithSuggestionError(impl, error.Message) :> _ else

            let node = nodeSelectionProvider.GetExpressionInRange<ITreeNode>(fsFile, range, false, null)
            match node.Parent with
            | :? IFSharpTypeDeclaration as typeDecl when typeDecl.Identifier == node ->
                NoImplementationGivenInTypeWithSuggestionError(typeDecl, error.Message) :> _

            | :? IInterfaceImplementation as impl when impl.TypeName == node ->
                NoImplementationGivenInInterfaceWithSuggestionError(impl, error.Message) :> _

            | :? ITypeReferenceName as typeName when
                    isNotNull (InterfaceImplementationNavigator.GetByTypeName(typeName)) ->
                let impl = InterfaceImplementationNavigator.GetByTypeName(typeName)
                NoImplementationGivenInInterfaceWithSuggestionError(impl, error.Message) :> _

            | _ ->

            match node with
            | :? IInterfaceImplementation as impl when
                    isNotNull (ObjExprNavigator.GetByInterfaceImplementation(impl)) ->
                NoImplementationGivenInInterfaceWithSuggestionError(impl, error.Message) :> _
            | :? IObjExpr as objExpr ->
                NoImplementationGivenInTypeWithSuggestionError(objExpr, error.Message) :> _
            | _ ->
                createGenericHighlighting error range

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

        | InvalidUseOfTypeName ->
            let identifier = fsFile.GetNode(range)
            let referenceOwner = FSharpReferenceOwnerNavigator.GetByIdentifier(identifier)
            if isNotNull referenceOwner then InvalidUseOfTypeNameError(referenceOwner.Reference, error.Message) :> _ else

            createGenericHighlighting error range

        | PropertyIsStatic ->
            createHighlightingFromNode PropertyIsStaticError range

        | PropertyCannotBeSet ->
            createHighlightingFromNode PropertyCannotBeSetError range

        | AttributeIsNotValidOnThisElement ->
            match fsFile.GetNode<IAttribute>(range) with
            | null -> null
            | attribute -> AttributeIsNotValidOnThisElementError(attribute, error.Message)

        | LocalClassBindingsCannotBeInline ->
            createHighlightingFromParentNode LocalClassBindingsCannotBeInlineError range

        | TypeAbbreviationsCannotHaveAugmentations ->
            // For `type Foo.Bar<'T> with ...` FCS reports `Foo.Bar` lid range, we're interested in `Bar` offset.
            createHighlightingFromNodeAtOffset TypeAbbreviationsCannotHaveAugmentationsError range.EndOffset.Offset

        | LetAndForNonRecBindings ->
            createHighlightingFromGrandparentNode LetAndForNonRecBindingsError range

        | UnusedThisVariable ->
            createHighlightingFromParentNode UnusedThisVariableWarning range
            
        | LiteralPatternDoesNotTakeArguments ->
            createHighlightingFromNode LiteralPatternDoesNotTakeArgumentsError range

        | ArgumentNamesInSignatureAndImplementationDoNotMatch ->
            match error.ExtendedData with
            | Some (:? ArgumentsInSigAndImplMismatchExtendedData as data) ->
                match nodeSelectionProvider.GetExpressionInRange(fsFile, range, false, null) with
                | null -> null
                | expr -> ArgumentNameMismatchWarning(expr, data.SignatureName, data.ImplementationName, error.Message) :> _

            | _ -> null

        | CantTakeAddressOfExpression ->
            createHighlightingFromNode CantTakeAddressOfExpressionError range

        | SingleQuoteInSingleQuote ->
            createHighlightingFromNodeWithMessage SingleQuoteInSingleQuoteError range error

        | ObjectOfIndeterminateTypeUsedRequireTypeConstraint ->
            createHighlightingFromNode IndexerIndeterminateTypeError range

        | AbstractTypeCannotBeInstantiated ->
            createHighlightingFromNodeWithMessage AbstractTypeCannotBeInstantiatedError range error

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

        | UnionCaseDoesNotTakeArguments ->
            createHighlightingFromNode UnionCaseDoesNotTakeArgumentsError range

        | UnionCaseExpectsTupledArguments ->
            createHighlightingFromNodeWithMessage UnionCaseExpectsTupledArgumentsError range error

        | ConstructRequiresListArrayOrSequence ->
            createHighlightingFromParentNode YieldRequiresSeqExpressionError range

        | ConstructRequiresComputationExpression ->
            createHighlightingFromParentNode ReturnRequiresComputationExpressionError range

        | EmptyRecordInvalid ->
            createHighlightingFromNodeWithMessage EmptyRecordInvalidError range error

        | MissingErrorNumber ->
            match error.ExtendedData with
            | Some (:? ExpressionIsAFunctionExtendedData) ->
                createHighlightingFromMappedExpression getResultExpr FunctionValueUnexpectedWarning range error

            | Some (:? FieldNotContainedDiagnosticExtendedData) ->
                createHighlightingFromParentNodeWithMessage FieldNotContainedTypesDifferError range error
            
            | Some (:? TypeMismatchDiagnosticExtendedData as data) ->
                if isUnit data.ExpectedType then
                    createHighlightingFromMappedExpression getResultExpr UnitTypeExpectedError range error else

                createTypeMismatchHighlighting TypeConstraintMismatchError range error

            | _ -> null

        | NamespaceCannotContainValues ->
            let binding = fsFile.GetNode<IBindingLikeDeclaration>(range)
            if isNotNull binding then NamespaceCannotContainBindingsError(binding) :> _ else

            let expr = fsFile.GetNode<IDoLikeStatement>(range)
            if isNotNull expr then NamespaceCannotContainExpressionsError(expr) :> _ else null

        | _ -> createGenericHighlighting error range

    abstract CacheDiagnostics: bool
    default this.CacheDiagnostics = false

    abstract ShouldAddDiagnostic: error: FSharpDiagnostic * range: DocumentRange -> bool
    default x.ShouldAddDiagnostic(error: FSharpDiagnostic, _) =
        error.ErrorNumber <> UnrecognizedOption &&
        error.ErrorNumber <> InvalidXmlDocPosition &&
        error.ErrorNumber <> XmlDocSignatureCheckFailed

    member x.Execute(errors: FSharpDiagnostic[], committer: Action<DaemonStageResult>) =
        let daemonProcess = x.DaemonProcess
        let sourceFile = daemonProcess.SourceFile
        let consumer = FilteringHighlightingConsumer(sourceFile, fsFile, daemonProcess.ContextBoundSettingsStore)

        let errors =
            errors
            |> Array.map (fun error -> error, getDocumentRange error)
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

        if x.CacheDiagnostics then
            fsFile.FcsCapturedInfo.SetCachedDiagnostics(cachedFcsDiagnostics)

        committer.Invoke(DaemonStageResult(consumer.CollectHighlightings()))
