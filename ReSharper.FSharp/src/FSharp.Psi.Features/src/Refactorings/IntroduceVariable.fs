namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.FSharpNamingService
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
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

type FSharpIntroduceVariable(workflow, solution, driver) =
    inherit IntroduceVariableBase(workflow, solution, driver)

    /// Applies to case where source expression is the node to replace and is the last expression in a block,
    /// i.e. it doesn't have any expression to put as InExpression in the new `let` binding expression.
    /// Producing incomplete expression adds error but is easier to edit code immediately afterwards.
    let alwaysGenerateCompleteBindingExpr = false

    static let needsSpaceAfterIdentNodeTypes =
        NodeTypeSet(
           ElementType.RECORD_EXPR,
           ElementType.ANON_RECORD_EXPR,
           ElementType.ARRAY_OR_LIST_EXPR,
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

    let rec getExprToInsertBefore (expr: IFSharpExpression): IFSharpExpression =
        let expr = expr.IgnoreParentParens()

        let parent = expr.Parent
        if isNull parent then expr else

        match parent with
        | :? IConditionOwnerExpr as conditionOwnerExpr when conditionOwnerExpr.ConditionExpr != expr -> expr 
        | :? IForLikeExpr as forLikeExpr when forLikeExpr.DoExpression == expr -> expr
        | :? ISequentialExpr | :? ILambdaExpr | :? ITryLikeExpr -> expr

        | :? IBinding as binding when
                binding.Expression == expr && isNotNull (LetOrUseExprNavigator.GetByBinding(binding)) &&

                // Don't escape bindings with expressions on separate lines
                let equalsToken = binding.EqualsToken
                isNotNull equalsToken && equalsToken.StartLine = expr.StartLine &&

                // Don't escape function declarations
                not (binding.HeadPattern :? IParametersOwnerPat) ->
            LetOrUseExprNavigator.GetByBinding(binding) :> _

        | :? IRecordFieldBinding as fieldBinding ->
            let recordExpr = RecordLikeExprNavigator.GetByFieldBinding(fieldBinding)
            getExprToInsertBefore recordExpr

        | :? ILetOrUseExpr as letExpr ->
            Assertion.Assert(letExpr.InExpression == expr, "letExpr.InExpression == expr")
            expr

        | :? IBinaryAppExpr as binaryAppExpr when
                binaryAppExpr.RightArgument == expr && isNotNull binaryAppExpr.LeftArgument ->
            let leftArgument = binaryAppExpr.LeftArgument

            if leftArgument.Indent = expr.Indent && leftArgument.EndLine + docLine 1 < expr.StartLine then
                // Don't move up from "blocks" after empty non-code line separators.
                // todo: allow choosing scope?
                expr
            else
                // Try going up from the left part instead.
                match leftArgument.IgnoreInnerParens() with
                | :? IBinaryAppExpr as binaryAppExpr when isNotNull binaryAppExpr.RightArgument ->
                    getExprToInsertBefore binaryAppExpr.RightArgument
                | _ -> getExprToInsertBefore leftArgument

        | :? IFSharpExpression as parentExpr -> getExprToInsertBefore parentExpr
        | _ -> expr

    let getCommonParentExpr (data: IntroduceVariableData) (sourceExpr: IFSharpExpression): IFSharpExpression =
        let commonParent = data.Usages.FindLCA().As<IFSharpExpression>().NotNull("commonParentExpr is null")

        let seqExpr = commonParent.As<ISequentialExpr>()
        if isNull seqExpr then commonParent else

        if sourceExpr.Parent == commonParent then sourceExpr else

        let contextExpr = sourceExpr.PathToRoot() |> Seq.find (fun n -> n.Parent == commonParent)
        contextExpr :?> _

    let getContextDeclaration (contextExpr: IFSharpExpression): IModuleMember =
        let binding = BindingNavigator.GetByExpression(contextExpr)
        if isNotNull binding && binding.HeadPattern :? IParametersOwnerPat then null else

        let letBindings = LetBindingsDeclarationNavigator.GetByBinding(binding)
        if isNotNull letBindings then letBindings :> _ else

        let doStmt = DoStatementNavigator.GetByExpression(contextExpr)
        if isNotNull doStmt && doStmt.IsImplicit then doStmt :> _ else null

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
            let matchClauseOwner = MatchClauseListOwnerNavigator.GetByClause(matchClause)
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
        let commonParentExpr = getCommonParentExpr data sourceExpr

        // `contextDecl` is not null when expression is bound to a module/type let binding
        let contextExpr = getExprToInsertBefore commonParentExpr
        let contextDecl = getContextDeclaration contextExpr

        let containingTypeElement =
            if isNull contextDecl then null else

            let typeDeclaration = contextDecl.GetContainingTypeDeclaration()
            if isNull typeDeclaration then null else

            typeDeclaration.DeclaredElement

        let contextIsSourceExpr = sourceExpr == contextExpr && isNull contextDecl
        let contextIsImplicitDo = sourceExpr == contextExpr && contextDecl :? IDoStatement
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

        let usedNames = FSharpNamingService.getUsedNames contextExpr data.Usages containingTypeElement
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

        let indentShift = contextExpr.Indent - sourceExpr.Indent
        shiftExpr indentShift sourceExpr

        let letBindings = createBinding contextExpr contextDecl name
        setBindingExpression sourceExpr contextIndent letBindings

        let replacedUsages, sourceExpr =
            data.Usages |> Seq.fold (fun ((replacedUsages, sourceExpr: IFSharpExpression) as acc) usage ->
                if not (isValid usage) then acc else

                let usageIsSourceExpr = usage == sourceExpr
                
                if usageIsSourceExpr && (removeSourceExpr || replaceSourceExprNode) then
                    // Ignore this usage, it's going to be removed via replacing tree ranges later.
                    acc else

                let refExpr = elementFactory.CreateReferenceExpr(name) :> IFSharpExpression
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

    static member IntroduceVar(expr: IFSharpExpression, textControl: ITextControl, ?removeSourceExpr) =
        let removeSourceExpr = defaultArg removeSourceExpr false

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

        let workflow = IntroduceVariableWorkflow(solution, null)
        RefactoringActionUtil.ExecuteRefactoring(dataContext, workflow)

    static member CanIntroduceVar(expr: IFSharpExpression) =
        if not (isValid expr) then false else

        let rec isValidExpr (expr: IFSharpExpression) =
            if FSharpMethodInvocationUtil.isNamedArgReference expr then false else

            match expr with
            | :? IReferenceExpr as refExpr ->
                let shortName = refExpr.ShortName
                if shortName = SharedImplUtil.MISSING_DECLARATION_NAME then false else

                if PrettyNaming.IsOperatorName shortName then false else

                let declaredElement = refExpr.Reference.Resolve().DeclaredElement
                not (declaredElement :? ITypeElement || declaredElement :? INamespace)

            | :? IParenExpr as parenExpr ->
                isValidExpr parenExpr.InnerExpression

            | :? IRangeSequenceExpr | :? IComputationExpr | :? IYieldOrReturnExpr -> false

            | _ -> true

        let isAllowedContext (expr: IFSharpExpression) =
            let topLevelExpr = skipIntermediateParentsOfSameType<IFSharpExpression>(expr)
            if isNotNull (AttributeNavigator.GetByExpression(topLevelExpr)) then false else

            true

        if not (isAllowedContext expr) then false else
        isValidExpr expr


type FSharpIntroduceVarHelper() =
    inherit IntroduceVariableHelper()

    let isExpressionToRemove (expr: ITreeNode) =
        expr.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemoveKey)

    override x.IsLanguageSupported = true

    override x.CheckAvailability(node) =
        let expr = node.As<IFSharpExpression>()
        if isNull expr || expr :? IFromErrorExpr then false else

        expr.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemoveKey) ||
        FSharpIntroduceVariable.CanIntroduceVar(expr)

    override x.CheckOccurrence(expr, occurrence) =
        isExpressionToRemove occurrence || not (isExpressionToRemove expr)
