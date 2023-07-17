namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open System.Collections.Generic
open System.Linq
open FSharp.Compiler.Symbols
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
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpNamingService
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
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
open JetBrains.UI.RichText
open JetBrains.Util

type FSharpIntroduceVariableWorkflow(solution, escapeLambdas: bool, addMutable: bool) =
    inherit IntroduceVariableWorkflow(solution, null)

    member val EscapeLambdas = escapeLambdas
    member val Mutable = addMutable


type FSharpIntroduceVariableData(sourceExpr, usages) =
    inherit IntroduceVariableData(sourceExpr, usages)

    member val FirstUsageExpr: IFSharpExpression = null with get, set
    member val ContextExpr: IFSharpExpression = null with get, set

    member val Keywords = [FSharpTokenType.LET] with get, set
    member val BindComputation = false with get, set
    member val OverridenType = null with get, set


module FSharpIntroduceVariable =
    let canInsertBeforeRightOperand (binaryAppExpr: IBinaryAppExpr) =
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
        let commonParent = data.Usages.FindLCA() :?> IFSharpExpression |> notNull

        let seqExpr = commonParent.As<ISequentialExpr>()
        if isNull seqExpr then commonParent else

        if sourceExpr.Parent == commonParent || sourceExpr == commonParent then sourceExpr else

        let contextExpr = sourceExpr.PathToRoot() |> Seq.find (fun n -> n.Parent == commonParent)
        contextExpr :?> _

    let getSafeParentExprToInsertBefore (workflow: IntroduceLocalWorkflowBase) (parent: IFSharpExpression) =
        match workflow with
        | :? FSharpIntroduceVariableWorkflow as fsWorkflow when fsWorkflow.EscapeLambdas ->
            getOutermostLambda parent
        | _ -> parent

    let getOccurrenceText (displayContext: FSharpDisplayContext) (fcsType: FSharpType) (text: string) =
        let richText = RichText("Bind '")
        richText.Append(fcsType.Format(displayContext), TextStyle(JetFontStyles.Bold)) |> ignore
        richText.Append(text, TextStyle()) |> ignore
        richText

    let getDeconstructionOccurrences fcsType displayContext (deconstruction: IFSharpDeconstruction) =
        let valueText = getOccurrenceText displayContext fcsType "' value"
        [| WorkflowPopupMenuOccurrence(valueText, null, null, null)
           WorkflowPopupMenuOccurrence(RichText(deconstruction.Text), null, deconstruction) |]


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

    let getNames (usedNames: ISet<string>) (data: FSharpIntroduceVariableData) (expr: IFSharpExpression) =
        createEmptyNamesCollection expr
        |> addNamesForExpression (Option.ofObj data.OverridenType) expr
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
                    if index = 0 then TreeRange(sequentialExpr) else
                    if removeSourceExpr then TreeRange(contextExpr, newSeqExpr) else TreeRange(newSeqExpr)

                {| ReplaceRange = replaceRange
                   InRange = TreeRange(newSeqExpr)
                   AddNewLine = not removeSourceExpr |}
            else
                let replaceRange = 
                    if index = 0 then TreeRange(sequentialExpr) else TreeRange(contextExpr, sequentialExpr.LastChild)
                
                // The last expression can be moved as is.
                {| ReplaceRange = replaceRange
                   InRange = inRange
                   AddNewLine = not removeSourceExpr |}
        else
            let range = TreeRange(contextExpr)
            {| ReplaceRange = range; InRange = range; AddNewLine = true |}

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

            clauses[index + 1].StartLine = matchClause.StartLine

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
            if indent > 0 then
                Whitespace(indent)
        ] |> ignore

    static member val ExpressionToRemoveKey = Key("FSharpIntroduceVariable.ExpressionToRemove")

    override x.Process(data) =
        let data = data :?> FSharpIntroduceVariableData
        let sourceExpr = data.FirstUsageExpr
        let contextExpr = data.ContextExpr

        // `contextDecl` is not null when expression is bound to a module/type let binding.
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

        let addSpaceNearIdents = needsSpaceAfterIdentNodeTypes[sourceExpr.NodeType]

        let containingTypeElement = getContainingType contextDecl
        let usedNames = getUsedNames [contextExpr] data.Usages containingTypeElement true
        let names = getNames usedNames data sourceExpr
        let name = if names.Count > 0 then names[0] else "x"

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
        let binding = letBindings.Bindings[0]

        // Replace the keyword after the parsing to workaround bad parser recovery for `let! x = ()` without in-expr
        // todo: fix parser recovery in Fcs
        ModificationUtil.ReplaceChild(binding.BindingKeyword, data.Keywords[0].CreateTreeElement()) |> ignore

        setBindingExpression sourceExpr contextIndent binding

        match workflow with
        | :? FSharpIntroduceVariableWorkflow as fsWorkflow when fsWorkflow.Mutable ->
            binding.SetIsMutable(true)
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
                    let argExpr = usage.IgnoreParentParens(includingBeginEndExpr = false)
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
                        if contextIndent > 0 then
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

                let binding = letBindings.Bindings[0]
                let nextSibling = binding.NextSibling

                let replaced =
                    if isNotNull nextSibling then
                        let replaceRange = TreeRange(nextSibling, letBindings.LastChild)
                        ModificationUtil.ReplaceChildRange(replaceRange, ranges.InRange)
                    else
                        ModificationUtil.AddChildRangeAfter(binding, ranges.InRange)

                if isInSingleLineContext then
                    addNodesBefore replaced.First [
                        Whitespace()
                        FSharpTokenType.IN.CreateLeafElement()
                        Whitespace()
                    ] |> ignore
                elif ranges.AddNewLine then
                    let anchor = ModificationUtil.AddChildBefore(replaced.First, NewLine(lineEnding))
                    if contextIndent > 0 then
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
                        if contextIndent > 0 then
                            Whitespace(contextIndent)
                    ] |> ignore
                    letBindings

            | _ -> failwithf "Unexpected let node type"

        let binding = letBindings.As<ILetBindings>().Bindings[0]

        match binding.Expression.IgnoreInnerParens() with
        | :? ILambdaExpr as lambdaExpr when not lambdaExpr.IsSingleLine ->
            // Use better indent for extracted lambda
            let bodyExpr = lambdaExpr.Expression
            let shift = lambdaExpr.Indent - bodyExpr.Indent + contextExpr.GetIndentSize()
            shiftWithWhitespaceBefore shift bodyExpr
        | _ -> ()
 
        let hotspotsRegistry = HotspotsRegistry(solution.GetPsiServices())

        let deconstruction =
            match workflow with
            | :? IntroduceVariableWorkflow as workflow ->
                workflow.DataModel.Deconstruction.As<IFSharpDeconstruction>()
            | _ -> null

        if isNotNull deconstruction then
            match FSharpDeconstructionImpl.deconstructImpl false deconstruction binding.HeadPattern with
            | Some(hotspotsRegistry, pattern) ->
                let node = pattern :> ITreeNode
                IntroduceVariableResult(hotspotsRegistry, node.CreateTreeElementPointer())
            | _ -> failwith "FSharpDeconstruction.deconstructImpl"
        else
            if data.Keywords.Length > 1 then
                let keywords = data.Keywords |> List.map (fun nodeType -> nodeType.TokenRepresentation)
                let suggestions = NameSuggestionsExpression(keywords)
                hotspotsRegistry.Register([| binding.BindingKeyword :> ITreeNode |], suggestions)

            let nodes =
                let replacedNodes =
                    replacedUsages
                    |> Seq.choose (fun pointer -> pointer.GetTreeNode() |> Option.ofObj)
                    |> Seq.toArray

                let pattern = binding.HeadPattern
                [| pattern :> ITreeNode |]
                |> Array.append replacedNodes

            let nameExpression = NameSuggestionsExpression(names)
            hotspotsRegistry.Register(nodes, nameExpression)

            let caretTarget =
                if isInSingleLineContext && replaceSourceExprNode then
                    letBindings.LastChild
                else
                    letBindings.Bindings[0].Expression :> _

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

            | :? IParenOrBeginEndExpr as parenExpr ->
                isNull (NewExprNavigator.GetByArgumentExpression(expr)) &&
                isAllowedExpr parenExpr.InnerExpression

            | :? IRangeLikeExpr | :? IIndexerExpr
            | :? IComputationExpr | :? IYieldOrReturnExpr
            | :? IFromErrorExpr -> false

            | :? ITupleExpr ->
                let parenOrBeginEndExpr = ParenOrBeginEndExprNavigator.GetByInnerExpression(expr)
                isNull (NewExprNavigator.GetByArgumentExpression(parenOrBeginEndExpr)) &&

                let listExpr = ListExprNavigator.GetByExpression(expr.IgnoreParentParens())
                let appExpr = PrefixAppExprNavigator.GetByArgumentExpression(listExpr)
                isNull appExpr || not appExpr.IsIndexerLike

            | :? IPrefixAppExpr as appExpr ->
                not appExpr.IsIndexerLike || isNull (SetExprNavigator.GetByLeftExpression(appExpr.IgnoreParentParens()))

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

type FSharpIntroduceVarHelper() =
    inherit IntroduceVariableHelper()

    let getApplicableBindingKeywords bindComputation supportsUse =
        match bindComputation, supportsUse with
        | false, true -> [FSharpTokenType.LET; FSharpTokenType.USE]
        | true, true -> [FSharpTokenType.LET_BANG; FSharpTokenType.USE_BANG]
        | false, _ -> [FSharpTokenType.LET]
        | true, _ -> [FSharpTokenType.LET_BANG]

    let getBuilderMethodParamTypes name (mfv: FSharpMemberOrFunctionOrValue) =
        if mfv.LogicalName <> name then None else

        let mfvParamGroups = mfv.CurriedParameterGroups
        if mfvParamGroups.Count <> 1 then None else

        let mfvParamGroup = mfvParamGroups[0]
        if mfvParamGroup.Count <> 2 then None else

        let binderType = mfvParamGroup[1].Type
        if not binderType.IsFunctionType then None else

        Some(mfvParamGroup[0].Type, binderType)

    override x.IsLanguageSupported = true

    override x.CheckAvailability(node) =
        FSharpIntroduceVariable.CanIntroduceVar(node.As<IFSharpExpression>(), false)

    override this.CreateData(sourceExpression, usages) =
        FSharpIntroduceVariableData(sourceExpression, usages) :> _

    override this.AdditionalInitialization(workflow, expression, context) =
        let data = workflow.DataModel :?> FSharpIntroduceVariableData

        // Replace the actual source expression with the outer-most expression among usages,
        // since it's needed for calculating a common node to replace.
        let sourceExpr = data.Usages |> Seq.minBy (fun u -> u.GetTreeStartOffset().Offset) :?> IFSharpExpression

        let commonParent = FSharpIntroduceVariable.getCommonParentExpr data sourceExpr
        let safeParentToInsertBefore = FSharpIntroduceVariable.getSafeParentExprToInsertBefore workflow commonParent
        let contextExpr = FSharpIntroduceVariable.getExprToInsertBefore safeParentToInsertBefore

        data.FirstUsageExpr <- sourceExpr
        data.ContextExpr <- contextExpr

        let fcsType = sourceExpr.TryGetFcsType()
        let displayContext = sourceExpr.TryGetFcsDisplayContext()
        if isNull fcsType || isNull displayContext then true else

        let compExpr, _ = tryGetEffectiveParentComputationExpression contextExpr
        let displayContext = displayContext.WithShortTypeNames(true)
        let isInComputationExpr = isNotNull compExpr
        let computationType = 
            if not isInComputationExpr then None else

            let prefixAppExpr = PrefixAppExprNavigator.GetByArgumentExpression(compExpr)
            let builderFcsType = prefixAppExpr.FunctionExpression.TryGetFcsType()
            if isNull builderFcsType then None else

            let builderFcsType =
                if builderFcsType.IsFunctionType then builderFcsType.GenericArguments[0] else builderFcsType

            let prefixAppExprFcsType = prefixAppExpr.TryGetFcsType()
            if isNull prefixAppExprFcsType then None else

            let fcsEntity = getAbbreviatedEntity builderFcsType.TypeDefinition
            let allMembers = fcsEntity.MembersFunctionsAndValues

            allMembers |> Seq.tryPick (fun mfv ->
                getBuilderMethodParamTypes "Bind" mfv |> Option.bind (fun (computationType, lambdaType) ->
                    if not lambdaType.IsFunctionType then None else

                    let substitution = FSharpExpectedTypesUtil.extractPartialSubstitution computationType fcsType
                    let paramTypeWithSubstitution = computationType.Instantiate(substitution)
                    if fcsType <> paramTypeWithSubstitution then None else

                    Some(lambdaType.Instantiate(substitution).GenericArguments[0], allMembers)))

        let boundType =
            match computationType with
            | None -> Some(fcsType, false)
            | Some(computationType, _) ->
                let occurrences =
                    let getOccurrenceText =
                        FSharpIntroduceVariable.getOccurrenceText displayContext

                    let computationText = getOccurrenceText computationType "' computation with let!"
                    let valueText = getOccurrenceText fcsType "' value"
                    [| WorkflowPopupMenuOccurrence(valueText, null, (fcsType, false))
                       WorkflowPopupMenuOccurrence(computationText, null, (computationType, true)) |]

                let selectedOccurrence = workflow.ShowOccurrences(occurrences, context)
                if isNull selectedOccurrence then None else
                    Some(selectedOccurrence.Entities.FirstOrDefault())

        match boundType with
        | None -> false
        | Some(boundType, bindComputation) ->

        let disposableType = sourceExpr.GetPredefinedType().IDisposable

        let mappedBoundType = boundType.MapType(expression)
        let isDisposable = mappedBoundType.IsSubtypeOf(disposableType)

        let supportsUse =
            match isDisposable, computationType with
            | false, _ -> false
            | _, None -> true
            | _, Some(_, builderMembers) ->

            // We don't check the actual parameter type here,
            // due to can't reuse `T -> IDisposable` substitution from `Bind`: the Ts are considered different by Fcs.
            // todo: find a way to compute common substitution
            builderMembers
            |> Seq.tryPick (getBuilderMethodParamTypes "Using")
            |> Option.isSome

        data.Keywords <- getApplicableBindingKeywords bindComputation supportsUse

        if bindComputation then
            data.BindComputation <- bindComputation
            data.OverridenType <- mappedBoundType

        let deconstruction = FSharpDeconstruction.tryGetDeconstruction expression boundType
        match deconstruction with
        | None -> true
        | Some(deconstruction) ->

        let occurrences = FSharpIntroduceVariable.getDeconstructionOccurrences fcsType displayContext deconstruction

        let selectedOccurrence = workflow.ShowOccurrences(occurrences, context)
        if isNull selectedOccurrence then false else

        data.Deconstruction <- selectedOccurrence.Entities.FirstOrDefault()
        true
