namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open System.Collections.Generic
open JetBrains.Application.DataContext
open JetBrains.Application.UI.Actions.ActionManager
open JetBrains.Diagnostics
open JetBrains.DocumentModel.DataContext
open JetBrains.Lifetimes
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.Refactorings
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Naming.Elements
open JetBrains.ReSharper.Psi.Naming.Extentions
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Naming.Settings
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

    let getNames (expr: IFSharpExpression) =
        let language = expr.Language
        let sourceFile = expr.GetSourceFile()

        let namingManager = solution.GetPsiServices().Naming
        let namesCollection =
            namingManager.Suggestion.CreateEmptyCollection(PluralityKinds.Unknown, language, true, sourceFile)

        let entryOptions = EntryOptions(subrootPolicy = SubrootPolicy.Decompose, prefixPolicy = PredefinedPrefixPolicy.Remove)
        namesCollection.Add(expr, entryOptions)

        let settingsStore = expr.GetSettingsStoreWithEditorConfig()
        let elementKind = NamedElementKinds.Locals
        let descriptor = ElementKindOfElementType.LOCAL_VARIABLE
        let namingRule =
            namingManager.Policy.GetDefaultRule(sourceFile, language, settingsStore, elementKind, descriptor)

        let suggestionOptions = SuggestionOptions(null, DefaultName = "foo")
        namesCollection.Prepare(namingRule, ScopeKind.Common, suggestionOptions).AllNames()

    let getReplaceRanges (sourceExpr: IFSharpExpression) (contextExpr: IFSharpExpression) removeSourceExpr =
        let sequentialExpr = SequentialExprNavigator.GetByExpression(contextExpr)
        if sourceExpr == contextExpr && isNotNull sequentialExpr then
            let inRangeStart = if removeSourceExpr then sourceExpr.NextSibling else sourceExpr :> _
            let inRange = TreeRange(inRangeStart, sequentialExpr.LastChild)

            let seqExprs = sequentialExpr.Expressions
            let index = seqExprs.IndexOf(sourceExpr)

            if seqExprs.Count - index > 2 then
                // Replace rest expressions with a sequential expr node.
                let newSeqExpr = ElementType.SEQUENTIAL_EXPR.Create()
                let newSeqExpr = ModificationUtil.ReplaceChildRange(inRange, TreeRange(newSeqExpr)).First

                LowLevelModificationUtil.AddChild(newSeqExpr, Array.ofSeq inRange)

                let replaceRange =
                    if removeSourceExpr then TreeRange(sourceExpr, newSeqExpr) else TreeRange(newSeqExpr)

                {| ReplaceRange = replaceRange
                   InRange = TreeRange(newSeqExpr)
                   AddNewLine = not removeSourceExpr |}
            else
                // The last expression can be moved as is.
                {| ReplaceRange = TreeRange(sourceExpr, sequentialExpr.LastChild)
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

                // Don't escape function declarations
                not (binding.HeadPattern :? IParametersOwnerPat) ->
            LetOrUseExprNavigator.GetByBinding(binding) :> _

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
        let letDecl = LetModuleDeclNavigator.GetByBinding(BindingNavigator.GetByExpression(contextExpr))
        if isNotNull letDecl then letDecl :> _ else

        let doDecl = DoNavigator.GetByExpression(contextExpr)
        if isNotNull doDecl && doDecl.IsImplicit then doDecl :> _ else null

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
            | :? IMemberDeclaration | :? IAutoProperty -> false
            | _ -> true

        let contextExprParent = contextExpr.Parent
        let contextParent = contextExpr.IgnoreParentChameleonExpr()

        if not contextExpr.IsSingleLine && requiresMultilineExpr contextParent then None else

        let prevToken =
            match contextParent with
            | :? IBinding as binding when isNotNull binding.Parent -> binding.EqualsToken
            | :? IMemberDeclaration as memberDeclaration -> memberDeclaration.EqualsToken
            | :? IAutoProperty as autoProperty -> autoProperty.EqualsToken
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

        let contextIsSourceExpr = sourceExpr == contextExpr && isNull contextDecl
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

        let names = getNames sourceExpr
        let name = if names.Count > 0 then names.[0] else "x"

        let removeSourceExpr =
            if data.SourceExpression.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemoveKey) then true else
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
            data.Usages |> Seq.fold (fun ((replacedUsages, sourceExpr) as acc) usage ->
                if not (isValid usage) then acc else

                let usageIsSourceExpr = usage == sourceExpr
                if usageIsSourceExpr && (removeSourceExpr || contextIsSourceExpr && not isInSeqExpr) then acc else

                let refExpr = elementFactory.CreateReferenceExpr(name)
                let replacedUsage = ModificationUtil.ReplaceChild(usage, refExpr)

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
                let ranges = getReplaceRanges sourceExpr contextExpr removeSourceExpr
                let replaced = ModificationUtil.ReplaceChildRange(ranges.ReplaceRange, TreeRange(letBindings))
                let letBindings = replaced.First :?> ILetBindings

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
                letBindings

            | :? ILetModuleDecl ->
                addNodesBefore contextDecl [
                    letBindings
                    NewLine(lineEnding)
                    Whitespace(contextIndent)
                ] |> ignore
                letBindings

            | _ -> failwithf "Unexpected let node type"

        let nodes =
            let replacedNodes =
                replacedUsages
                |> Seq.choose (fun pointer -> pointer.GetTreeNode() |> Option.ofObj)
                |> Seq.toArray

            [| letBindings.As<ILetBindings>().Bindings.[0].HeadPattern :> ITreeNode |]
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
            if isNotNull (FSharpMethodInvocationUtil.tryGetNamedArg expr) then false else

            match expr with
            | :? IReferenceExpr as refExpr ->
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
        if isNull expr then false else

        if expr.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemoveKey) then true else

        if not (expr.FSharpExperimentalFeaturesEnabled()) then false else
        FSharpIntroduceVariable.CanIntroduceVar(expr)

    override x.CheckOccurrence(expr, occurrence) =
        if isExpressionToRemove occurrence then true else

        if isExpressionToRemove expr then false else
        expr.FSharpExperimentalFeaturesEnabled()
