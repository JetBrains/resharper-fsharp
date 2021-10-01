namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open System.Collections.Generic
open FSharp.Compiler.Syntax
open JetBrains.Application.DataContext
open JetBrains.Application.UI.Actions.ActionManager
open JetBrains.Diagnostics
open JetBrains.DocumentModel.DataContext
open JetBrains.Lifetimes
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.Refactorings
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpNamingService
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Pointers
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Refactorings.IntroduceVariable
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.TextControl.DataContext
open JetBrains.Util

type FSharpIntroduceVariableWorkflow(solution, escapeLambdas: bool, addMutable: bool) =
    inherit IntroduceVariableWorkflow(solution, null)

    member val EscapeLambdas = escapeLambdas
    member val Mutable = addMutable

type FSharpIntroduceVariable(workflow: IntroduceLocalWorkflowBase, solution, driver) =
    inherit IntroduceVariableBase(workflow, solution, driver)

    /// Applies to case where source expression is the node to replace and is the last expression in a block,
    /// i.e. it doesn't have any expression to put as InExpression in the new `let` binding expression.
    /// Producing incomplete expression adds error but is easier to edit code immediately afterwards.
    let alwaysGenerateCompleteBindingExpr = false

    static let needsSpaceAfterIdentNodeTypes =
        NodeTypeSet(
           ElementType.RECORD_EXPR,
           ElementType.ANON_RECORD_EXPR,
           ElementType.ARRAY_EXPR,
           ElementType.LIST_EXPR,
           ElementType.PAREN_EXPR,
           ElementType.LAMBDA_EXPR,
           ElementType.MATCH_LAMBDA_EXPR,
           ElementType.COMPUTATION_EXPR,
           ElementType.QUOTE_EXPR,
           ElementType.OBJ_EXPR,
           ElementType.ADDRESS_OF_EXPR)

    let getNames (usedNames: ISet<string>) (expr: IFSharpExpression) =
        createEmptyNamesCollection expr
        |> addNamesForExpression expr
        |> prepareNamesCollection usedNames expr

    let getReplaceRanges (contextExpr: IFSharpExpression) removeSourceExpr =
        let sequentialExpr = SequentialExprNavigator.GetByExpression(contextExpr)
        if isNotNull sequentialExpr then
            let inRangeStart = if removeSourceExpr then contextExpr.NextSibling else contextExpr :> _
            let inRange = TreeRange(inRangeStart, sequentialExpr.LastChild)

            let seqExprs = sequentialExpr.Expressions
            let index = seqExprs.IndexOf(contextExpr)

            if seqExprs.Count - index > 2 then
                // Replace rest expressions with a sequential expr node.
                let newSeqExpr = ElementType.SEQUENTIAL_EXPR.Create()
                let newSeqExpr = ModificationUtil.ReplaceChildRange(inRange, TreeRange(newSeqExpr)).First

                LowLevelModificationUtil.AddChild(newSeqExpr, Array.ofSeq inRange)

                let replaceRange =
                    if removeSourceExpr then TreeRange(contextExpr, newSeqExpr) else TreeRange(newSeqExpr)

                {| ReplaceRange = replaceRange
                   InRange = TreeRange(newSeqExpr)
                   AddNewLine = not removeSourceExpr |}
            else
                // The last expression can be moved as is.
                {| ReplaceRange = TreeRange(contextExpr, sequentialExpr.LastChild)
                   InRange = inRange
                   AddNewLine = not removeSourceExpr |}
        else
            let range = TreeRange(contextExpr)
            {| ReplaceRange = range; InRange = range; AddNewLine = true |}

    static let canInsertBeforeRightOperand (binaryAppExpr: IBinaryAppExpr) =
        // Don't move up from "blocks" after empty non-code line separators.
        // todo: allow choosing scope?

        let leftArgument = binaryAppExpr.LeftArgument
        let rightArgument = binaryAppExpr.RightArgument
        isNotNull leftArgument && isNotNull rightArgument &&

        leftArgument.Indent = rightArgument.Indent && leftArgument.EndLine + docLine 1 < rightArgument.StartLine

    let rec getOutermostLambda (node: IFSharpExpression) =
        match node.GetContainingNode<ILambdaExpr>() with
        | null -> node
        | lambdaExpr -> getOutermostLambda lambdaExpr

    let rec getExprToInsertBefore (expr: IFSharpExpression): IFSharpExpression =
        let expr = expr.IgnoreParentParens()

        let parent = expr.Parent
        if isNull parent then expr else

        match parent with
        | :? IConditionOwnerExpr as conditionOwnerExpr when conditionOwnerExpr.ConditionExpr != expr -> expr
        | :? IForLikeExpr as forLikeExpr when forLikeExpr.DoExpression == expr -> expr
        | :? ISequentialExpr | :? ILambdaExpr | :? ITryLikeExpr | :? IComputationExpr -> expr

        | :? IBinding as binding when
                binding.Expression == expr && isNotNull (LetOrUseExprNavigator.GetByBinding(binding)) &&

                // Don't escape bindings with expressions on separate lines
                let equalsToken = binding.EqualsToken
                isNotNull equalsToken && equalsToken.StartLine = expr.StartLine &&

                // Don't escape function declarations
                not binding.HasParameters ->
            LetOrUseExprNavigator.GetByBinding(binding) :> _

        | :? IRecordFieldBinding as fieldBinding ->
            let recordExpr = RecordLikeExprNavigator.GetByFieldBinding(fieldBinding)
            getExprToInsertBefore recordExpr

        | :? ILocalBinding as binding when
                isNotNull (LetOrUseExprNavigator.GetByBinding(binding)) &&
                binding.ParameterPatternsEnumerable.IsEmpty() ->
            LetOrUseExprNavigator.GetByBinding(binding) :> _

        | :? ILetOrUseExpr as letExpr ->
            Assertion.Assert(letExpr.InExpression == expr, "letExpr.InExpression == expr")
            expr

        | :? IBinaryAppExpr as binaryAppExpr when
                binaryAppExpr.RightArgument == expr && isNotNull binaryAppExpr.LeftArgument ->
            if canInsertBeforeRightOperand binaryAppExpr then
                expr
            else
                // Try going up from the left part instead.
                let leftArgument = binaryAppExpr.LeftArgument
                match leftArgument.IgnoreInnerParens() with
                | :? IBinaryAppExpr as binaryAppExpr when isNotNull binaryAppExpr.RightArgument ->
                    getExprToInsertBefore binaryAppExpr.RightArgument
                | _ -> getExprToInsertBefore leftArgument

        | :? IIndexerArgList as indexerArgList ->
            let indexerExpr = ItemIndexerExprNavigator.GetByIndexerArgList(indexerArgList)
            if isNull indexerExpr then expr else indexerExpr :> _

        | :? IFSharpExpression as parentExpr -> getExprToInsertBefore parentExpr
        | _ -> expr

    let getCommonParentExpr (data: IntroduceVariableData) (sourceExpr: IFSharpExpression): IFSharpExpression =
        let commonParent = data.Usages.FindLCA().As<IFSharpExpression>().NotNull("commonParentExpr is null")

        let seqExpr = commonParent.As<ISequentialExpr>()
        if isNull seqExpr then commonParent else

        if sourceExpr.Parent == commonParent || sourceExpr == commonParent then sourceExpr else

        let contextExpr = sourceExpr.PathToRoot() |> Seq.find (fun n -> n.Parent == commonParent)
        contextExpr :?> _

    let getSafeParentExprToInsertBefore (parent: IFSharpExpression) =
        match workflow with
        | :? FSharpIntroduceVariableWorkflow as fsWorkflow when fsWorkflow.EscapeLambdas ->
            getOutermostLambda parent
        | _ -> parent

    let getContextDeclaration (contextExpr: IFSharpExpression): IModuleMember =
        let binding = BindingNavigator.GetByExpression(contextExpr)
        if isNotNull binding && binding.HasParameters then null else

        let letBindings = LetBindingsDeclarationNavigator.GetByBinding(binding)
        if isNotNull letBindings then letBindings :> _ else

        let exprStmt = ExpressionStatementNavigator.GetByExpression(contextExpr)
        if isNotNull exprStmt then exprStmt :> _ else null

    let createBinding (context: IFSharpExpression) (contextDecl: IModuleMember) name: ILetBindings =
        let elementFactory = context.CreateElementFactory()
        if isNotNull contextDecl then
            elementFactory.CreateLetModuleDecl(name) :> _
        else
            elementFactory.CreateLetBindingExpr(name) :> _

    let isSingleLineContext (context: ITreeNode): bool =
        let contextParent = context.Parent
        if not contextParent.IsSingleLine then false else

        match contextParent with
        | :? IMatchClause as matchClause ->
            let matchClauseOwner = MatchClauseListOwnerExprNavigator.GetByClause(matchClause)
            if matchClauseOwner.IsSingleLine then true else

            let clauses = matchClauseOwner.Clauses
            let index = clauses.IndexOf(matchClause)
            if index = clauses.Count - 1 then false else

            clauses.[index + 1].StartLine = matchClause.StartLine

        | :? IIfThenElseExpr | :? ILambdaExpr | :? ITryLikeExpr | :? IWhenExprClause -> true
        | _ -> false

    let getMoveToNewLineInfo (contextExpr: IFSharpExpression) =
        let requiresMultilineExpr (parent: ITreeNode) =
            match parent with
            | :? IMemberDeclaration | :? IAutoPropertyDeclaration -> false
            | _ -> true

        let contextExprParent = contextExpr.Parent
        let contextParent = contextExpr.IgnoreParentChameleonExpr()

        if not contextExpr.IsSingleLine && requiresMultilineExpr contextParent then None else

        let prevToken =
            match contextParent with
            | :? IBinding as binding when isNotNull binding.Parent -> binding.EqualsToken
            | :? IMemberDeclaration as memberDeclaration -> memberDeclaration.EqualsToken
            | :? IAutoPropertyDeclaration as autoProperty -> autoProperty.EqualsToken
            | :? IMatchClause as matchClause -> matchClause.RArrow
            | :? IWhenExprClause as whenExpr -> whenExpr.WhenKeyword
            | :? ILambdaExpr as lambdaExpr -> lambdaExpr.RArrow
            | :? ITryLikeExpr as tryExpr -> tryExpr.TryKeyword
            | _ -> null

        if isNull prevToken then None else

        let contextExpr: IFSharpTreeNode =
            if contextExprParent :? IChameleonExpression then contextExprParent :?> _ else contextExpr :> _

        let prevSignificant = skipMatchingNodesBefore isInlineSpaceOrComment contextExpr
        if prevSignificant != prevToken then None else

        let indent =
            match contextParent with
            | :? IBinding -> contextParent.Parent.Indent
            | _ -> contextParent.Indent

        Some(indent + contextExpr.GetIndentSize())

    let moveToNewLine (contextExpr: IFSharpExpression) (indent: int) =
        let prevSibling = contextExpr.PrevSibling
        if isInlineSpace prevSibling then
            let first = getFirstMatchingNodeBefore isInlineSpace prevSibling
            ModificationUtil.DeleteChildRange(first, prevSibling)

        addNodesBefore contextExpr [
            NewLine(contextExpr.GetLineEnding())
            Whitespace(indent)
        ] |> ignore

    static member val ExpressionToRemoveKey = Key("FSharpIntroduceVariable.ExpressionToRemove")

    override x.Process(data) =
        // Replace the actual source expression with the outer-most expression among usages,
        // since it's needed for calculating a common node to replace.
        let sourceExpr = data.Usages |> Seq.minBy (fun u -> u.GetTreeStartOffset().Offset) :?> IFSharpExpression
        let commonParent = getCommonParentExpr data sourceExpr
        let safeParentToInsertBefore = getSafeParentExprToInsertBefore commonParent

        // `contextDecl` is not null when expression is bound to a module/type let binding.
        let contextExpr = getExprToInsertBefore safeParentToInsertBefore
        let contextDecl = getContextDeclaration contextExpr

        let contextIsSourceExpr = sourceExpr == contextExpr && isNull contextDecl
        let contextIsImplicitDo = sourceExpr == contextExpr && contextDecl :? IDoLikeStatement
        let isInSingleLineContext = isNull contextDecl && isSingleLineContext contextExpr

        let isInSeqExpr =
            let seqExpr = SequentialExprNavigator.GetByExpression(sourceExpr)
            isNotNull seqExpr && sourceExpr != seqExpr.Expressions.Last()

        let replaceSourceExprNode = contextIsSourceExpr && not isInSeqExpr

        let moveToNewLineInfo =
            if isNotNull contextDecl || isInSingleLineContext then None else getMoveToNewLineInfo contextExpr

        let contextIndent =
            if isNotNull contextDecl then contextDecl.Indent else

            match moveToNewLineInfo with
            | Some indent -> indent
            | _ -> contextExpr.Indent

        let addSpaceNearIdents = needsSpaceAfterIdentNodeTypes.[sourceExpr.NodeType]

        let containingTypeElement = getContainingType contextDecl
        let usedNames = getUsedNames [contextExpr] data.Usages containingTypeElement true
        let names = getNames usedNames sourceExpr
        let name = if names.Count > 0 then names.[0] else "x"

        let removeSourceExpr =
            if data.SourceExpression.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemoveKey) then true else
            if contextIsImplicitDo then true else
            if not contextIsSourceExpr then false else
            if data.Usages.Count = 1 then true else

            let seqExpr = SequentialExprNavigator.GetByExpression(sourceExpr)
            if isNull seqExpr then false else

            let arrayOrListExpr = ArrayOrListExprNavigator.GetByExpression(seqExpr)
            isNull arrayOrListExpr || data.Usages.Count = 1

        sourceExpr.UserData.RemoveKey(FSharpIntroduceVariable.ExpressionToRemoveKey)

        use writeCookie = WriteLockCookie.Create(sourceExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let lineEnding = sourceExpr.GetLineEnding()
        let elementFactory = sourceExpr.CreateElementFactory()

        let indentShift = contextIndent - sourceExpr.Indent
        shiftNode indentShift sourceExpr

        let letBindings = createBinding contextExpr contextDecl name
        setBindingExpression sourceExpr contextIndent letBindings

        match workflow with
        | :? FSharpIntroduceVariableWorkflow as fsWorkflow when fsWorkflow.Mutable ->
            letBindings.Bindings.[0].SetIsMutable(true)
        | _ -> ()

        let replacedUsages, sourceExpr =
            data.Usages |> Seq.fold (fun (replacedUsages, sourceExpr: IFSharpExpression as acc) usage ->
                let usage = usage.As<IFSharpExpression>()
                if not (isValid usage) then acc else

                let usageIsSourceExpr = usage == sourceExpr

                if usageIsSourceExpr && (removeSourceExpr || replaceSourceExprNode) then
                    // Ignore this usage, it's going to be removed via replacing tree ranges later.
                    acc else

                let refExpr = elementFactory.CreateReferenceExpr(name) :> IFSharpExpression

                let usage =
                    // remove parens in `not ({selstart}v.M(){selend})`, so it becomes `not x`
                    let argExpr = usage.IgnoreParentParens()
                    let appExpr = PrefixAppExprNavigator.GetByArgumentExpression(argExpr)
                    let funExpr = if isNotNull appExpr then appExpr.FunctionExpression else null

                    if argExpr != usage && isNotNull funExpr && funExpr.NextSibling != argExpr then argExpr else usage

                let replacedUsage = ModificationUtil.ReplaceChild(usage, refExpr)

                if addSpaceNearIdents then
                    if replacedUsage.GetPreviousToken().IsIdentifier() then
                        ModificationUtil.AddChildBefore(replacedUsage, Whitespace()) |> ignore

                    if replacedUsage.GetNextToken().IsIdentifier() then
                        ModificationUtil.AddChildAfter(replacedUsage, Whitespace()) |> ignore

                let sourceExpr =
                    if usageIsSourceExpr && contextIsSourceExpr && isInSeqExpr then replacedUsage else sourceExpr

                let replacedUsagePointer = replacedUsage.As<ITreeNode>().CreateTreeElementPointer()
                replacedUsagePointer :: replacedUsages, sourceExpr) ([], sourceExpr)

        let contextExpr = if contextIsSourceExpr then sourceExpr else contextExpr
        let replacedUsages = List(Seq.rev replacedUsages)

        match moveToNewLineInfo with
        | Some indent -> moveToNewLine contextExpr indent
        | _ -> ()

        let letBindings: ILetBindings =
            match letBindings with
            | :? ILetOrUseExpr when replaceSourceExprNode ->
                let letBindings = ModificationUtil.ReplaceChild(sourceExpr, letBindings)

                let createRefExpr () =
                    let refExpr = elementFactory.CreateReferenceExpr(name).As<ITreeNode>()
                    let nodePointer = refExpr.CreateTreeElementPointer()
                    replacedUsages.Add(nodePointer)
                    refExpr

                if alwaysGenerateCompleteBindingExpr then
                    addNodesAfter letBindings.LastChild [
                        NewLine(lineEnding)
                        Whitespace(contextIndent)

                        if removeSourceExpr then
                            elementFactory.CreateExpr("()")
                        else
                            createRefExpr ()
                    ] |> ignore

                if isInSingleLineContext then
                    addNodesAfter letBindings.LastChild [
                        Whitespace()
                        FSharpTokenType.IN.CreateLeafElement()
                        Whitespace()
                        createRefExpr ()
                    ] |> ignore

                letBindings

            | :? ILetOrUseExpr ->
                let ranges = getReplaceRanges contextExpr removeSourceExpr
                let letBindings = ModificationUtil.AddChildBefore(ranges.ReplaceRange.First, letBindings)

                let binding = letBindings.Bindings.[0]
                let replaceRange = TreeRange(binding.NextSibling, letBindings.LastChild)
                let replaced = ModificationUtil.ReplaceChildRange(replaceRange, ranges.InRange)

                if isInSingleLineContext then
                    addNodesBefore replaced.First [
                        Whitespace()
                        FSharpTokenType.IN.CreateLeafElement()
                        Whitespace()
                    ] |> ignore
                elif ranges.AddNewLine then
                    let anchor = ModificationUtil.AddChildBefore(replaced.First, NewLine(lineEnding))
                    ModificationUtil.AddChildAfter(anchor, Whitespace(contextIndent)) |> ignore

                ModificationUtil.DeleteChildRange(ranges.ReplaceRange)
                letBindings

            | :? ILetBindingsDeclaration ->
                if removeSourceExpr then
                    ModificationUtil.ReplaceChild(contextDecl, letBindings)
                else
                    let letBindings = ModificationUtil.AddChildBefore(contextDecl, letBindings)
                    addNodesAfter letBindings [
                        NewLine(lineEnding)
                        Whitespace(contextIndent)
                    ] |> ignore
                    letBindings

            | _ -> failwithf "Unexpected let node type"

        let binding = letBindings.As<ILetBindings>().Bindings.[0]

        match binding.Expression.IgnoreInnerParens() with
        | :? ILambdaExpr as lambdaExpr when not lambdaExpr.IsSingleLine ->
            // Use better indent for extracted lambda
            let bodyExpr = lambdaExpr.Expression
            let shift = lambdaExpr.Indent - bodyExpr.Indent + contextExpr.GetIndentSize()
            shiftWithWhitespaceBefore shift bodyExpr
        | _ -> ()

        let nodes =
            let replacedNodes =
                replacedUsages
                |> Seq.choose (fun pointer -> pointer.GetTreeNode() |> Option.ofObj)
                |> Seq.toArray

            [| binding.HeadPattern :> ITreeNode |]
            |> Array.append replacedNodes

        let nameExpression = NameSuggestionsExpression(names)
        let hotspotsRegistry = HotspotsRegistry(solution.GetPsiServices())
        hotspotsRegistry.Register(nodes, nameExpression)

        let caretTarget =
            if isInSingleLineContext && replaceSourceExprNode then
                letBindings.LastChild
            else
                letBindings.Bindings.[0].Expression :> _

        IntroduceVariableResult(hotspotsRegistry, caretTarget.CreateTreeElementPointer())

    static member IntroduceVar(expr: IFSharpExpression, textControl: ITextControl, removeSourceExpr, escapeLambdas,
            addMutable) =
        let name = "FSharpIntroduceVar"
        let solution = expr.GetSolution()

        let rules =
            DataRules
                .AddRule(name, ProjectModelDataConstants.SOLUTION, solution)
                .AddRule(name, DocumentModelDataConstants.DOCUMENT, textControl.Document)
                .AddRule(name, TextControlDataConstants.TEXT_CONTROL, textControl)
                .AddRule(name, PsiDataConstants.SELECTED_EXPRESSION, expr)

        use lifetime = Lifetime.Define(Lifetime.Eternal)

        let actionManager = Shell.Instance.GetComponent<IActionManager>()
        let dataContext = actionManager.DataContexts.CreateWithDataRules(lifetime.Lifetime, rules)

        if removeSourceExpr then
            expr.UserData.PutKey(FSharpIntroduceVariable.ExpressionToRemoveKey)

        let workflow = FSharpIntroduceVariableWorkflow(solution, escapeLambdas, addMutable)
        RefactoringActionUtil.ExecuteRefactoring(dataContext, workflow)

    static member CanIntroduceVar(expr: IFSharpExpression, checkQualifier: bool) =
        if not (isValid expr) || expr.IsFSharpSigFile() then false else

        let isAllowedRefExpr allowNull (refExpr: IReferenceExpr) =
            let declaredElement = refExpr.Reference.Resolve().DeclaredElement

            // the null case is checked separately,
            // since FCS doesn't return a symbol for entities in expr until they qualify a valid expression symbol
            allowNull && isNull declaredElement ||
            isNotNull declaredElement && not (declaredElement :? ITypeElement || declaredElement :? INamespace)

        let rec isAllowedExpr (expr: IFSharpExpression) =
            if FSharpMethodInvocationUtil.isNamedArgReference expr then false else

            match expr with
            | :? IReferenceExpr as refExpr ->
                let shortName = refExpr.ShortName
                if shortName = SharedImplUtil.MISSING_DECLARATION_NAME then false else

                if PrettyNaming.IsOperatorDisplayName shortName then false else

                if not checkQualifier then
                    isAllowedRefExpr true refExpr
                else
                    match refExpr.Qualifier with
                    | null -> false
                    | :? IReferenceExpr as refExpr -> isAllowedRefExpr false refExpr
                    | _ -> true

            | :? IParenExpr as parenExpr ->
                isNull (NewExprNavigator.GetByArgumentExpression(expr)) &&
                isAllowedExpr parenExpr.InnerExpression

            | :? IRangeLikeExpr | :? IComputationExpr | :? IYieldOrReturnExpr | :? IFromErrorExpr -> false

            | :? ITupleExpr ->
                isNull (NewExprNavigator.GetByArgumentExpression(ParenExprNavigator.GetByInnerExpression(expr)))

            | _ -> true

        let isAllowedContext (expr: IFSharpExpression) =
            let topLevelExpr = skipIntermediateParentsOfSameType<IFSharpExpression>(expr)
            isNull (AttributeNavigator.GetByExpression(topLevelExpr)) &&

            match expr with
            | :? ITupleExpr ->
                isNull (ItemIndexerExprNavigator.GetByIndexerArgList(IndexerArgListNavigator.GetByArg(expr))) &&
                isNull (NamedIndexerExprNavigator.GetByArg(TupleExprNavigator.GetByExpression expr))
            | _ -> true

        if not (isAllowedContext expr) then false else
        isAllowedExpr expr

    static member IsValidInnerExpression(expr: IExpression) =
        let expr = expr.As<IFSharpExpression>()
        if isNull expr then false else

        match expr with
        | :? ILetOrUseExpr
        | :? ISequentialExpr -> false
        | _ ->

        let aprExpr = PrefixAppExprNavigator.GetByArgumentExpression(expr)
        not (isNotNull aprExpr && aprExpr.IsHighPrecedence)

    static member CanInsertBeforeRightOperand(binaryAppExpr: IBinaryAppExpr) =
        canInsertBeforeRightOperand binaryAppExpr

type FSharpIntroduceVarHelper() =
    inherit IntroduceVariableHelper()

    override x.IsLanguageSupported = true

    override x.CheckAvailability(node) =
        FSharpIntroduceVariable.CanIntroduceVar(node.As<IFSharpExpression>(), false)
