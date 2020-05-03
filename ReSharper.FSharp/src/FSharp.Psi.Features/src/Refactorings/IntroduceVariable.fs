namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open JetBrains.Application.DataContext
open JetBrains.Application.UI.Actions.ActionManager
open JetBrains.DocumentModel.DataContext
open JetBrains.Lifetimes
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.Refactorings
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
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

    let getReplaceRanges (expr: IFSharpExpression) (parent: IFSharpExpression) =
        let sequentialExpr = SequentialExprNavigator.GetByExpression(expr)
        if expr == parent && isNotNull sequentialExpr then
            let inRange = TreeRange(expr.NextSibling, sequentialExpr.LastChild)

            let seqExprs = sequentialExpr.Expressions
            let index = seqExprs.IndexOf(expr)

            if seqExprs.Count - index > 2 then
                // Replace rest expressions with a sequential expr node.
                let newSeqExpr = ElementType.SEQUENTIAL_EXPR.Create()
                let newSeqExpr = ModificationUtil.ReplaceChildRange(inRange, TreeRange(newSeqExpr)).First

                LowLevelModificationUtil.AddChild(newSeqExpr, Array.ofSeq inRange)
                {| ReplaceRange = TreeRange(expr, newSeqExpr)
                   InRange = TreeRange(newSeqExpr)
                   AddNewLine = false |}
            else
                // The last expression can be moved as is.
                {| ReplaceRange = TreeRange(expr, sequentialExpr.LastChild)
                   InRange = inRange
                   AddNewLine = false |}
        else
            let range = TreeRange(parent)
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
                binding.Expression == expr && isNotNull (LetLikeExprNavigator.GetByBinding(binding)) &&

                // Don't escape function declarations
                not (binding.HeadPattern :? IParametersOwnerPat) ->
            LetLikeExprNavigator.GetByBinding(binding) :> _

        | :? IFSharpExpression as parentExpr -> getExprToInsertBefore parentExpr
        | _ -> expr

    let getContextDeclaration (contextExpr: IFSharpExpression): IModuleMember =
        let letDecl = LetModuleDeclNavigator.GetByBinding(BindingNavigator.GetByExpression(contextExpr))
        if isNotNull letDecl then letDecl :> _ else

        let doDecl = DoNavigator.GetByExpression(contextExpr)
        if isNotNull doDecl && doDecl.IsImplicit then doDecl :> _ else null

    let createBinding (context: IFSharpExpression) (contextDecl: IModuleMember) name: ILet =
        let elementFactory = context.CreateElementFactory()
        if isNotNull contextDecl then
            elementFactory.CreateLetModuleDecl(name) :> _
        else
            elementFactory.CreateLetBindingExpr(name) :> _

    let getMoveToNewLineInfo (contextExpr: IFSharpExpression) =
        if not contextExpr.IsSingleLine then None else

        let parent = contextExpr.Parent
        let prevToken =
            match parent with
            | :? IBinding as binding when isNotNull binding.Parent -> binding.EqualsToken
            | :? IMatchClause as matchClause -> matchClause.RArrow
            | :? ILambdaExpr as lambdaExpr -> lambdaExpr.RArrow
            | :? ITryLikeExpr as tryExpr -> tryExpr.TryKeyword
            | _ -> null

        if isNull prevToken then None else

        let prevSignificant = skipMatchingNodesBefore isInlineSpaceOrComment contextExpr
        if not (obj.ReferenceEquals(prevSignificant, prevToken)) then None else

        let indent =
            match parent with
            | :? IBinding -> parent.Parent.Indent
            | _ -> parent.Indent

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

    static member val ExpressionToRemove = Key("FSharpIntroduceVariable.ExpressionToRemove")

    override x.Process(data) =
        let sourceExpr = data.SourceExpression.As<IFSharpExpression>()
        let commonParentExpr = data.Usages.FindLCA().As<IFSharpExpression>()

        // contextDecl is not null when expression is bound to a module/type let binding
        let contextExpr = getExprToInsertBefore commonParentExpr
        let contextDecl = getContextDeclaration contextExpr
        let moveToNewLineInfo = if isNotNull contextDecl then None else getMoveToNewLineInfo contextExpr

        let contextIndent =
            if isNotNull contextDecl then contextDecl.Indent else

            match moveToNewLineInfo with
            | Some indent -> indent
            | _ -> contextExpr.Indent

        let names = getNames sourceExpr
        let name = if names.Count > 0 then names.[0] else "x"

        let removeSourceExpr = sourceExpr.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemove)
        sourceExpr.UserData.RemoveKey(FSharpIntroduceVariable.ExpressionToRemove)

        use writeCookie = WriteLockCookie.Create(sourceExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let lineEnding = sourceExpr.GetLineEnding()
        let elementFactory = sourceExpr.CreateElementFactory()

        let indentShift = contextExpr.Indent - sourceExpr.Indent
        shiftExpr indentShift sourceExpr

        let letBindings = createBinding contextExpr contextDecl name
        setBindingExpression sourceExpr contextIndent letBindings

        let replacedUsages =
            data.Usages |> Seq.choose (fun usage ->
                if not (isValid usage) then None else
                if removeSourceExpr && obj.ReferenceEquals(usage, contextExpr) then None else

                let ref = elementFactory.CreateReferenceExpr(name)
                Some (ModificationUtil.ReplaceChild(usage, ref).As<ITreeNode>().CreateTreeElementPointer()))
            |> Seq.toArray

        match moveToNewLineInfo with
        | Some indent -> moveToNewLine contextExpr indent
        | _ -> ()

        let letBindings: ILet = 
            match letBindings with
            | :? ILetOrUseExpr ->
                let ranges = getReplaceRanges sourceExpr contextExpr
                let replaced = ModificationUtil.ReplaceChildRange(ranges.ReplaceRange, TreeRange(letBindings))
                let letBindings = replaced.First :?> ILet

                let binding = letBindings.Bindings.[0]
                let replaceRange = TreeRange(binding.NextSibling, letBindings.LastChild)
                let replaced = ModificationUtil.ReplaceChildRange(replaceRange, ranges.InRange)

                if ranges.AddNewLine then
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
                |> Array.choose (fun pointer -> pointer.GetTreeNode() |> Option.ofObj)

            [| letBindings.As<ILet>().Bindings.[0].HeadPattern :> ITreeNode |]
            |> Array.append replacedNodes 

        let nameExpression = NameSuggestionsExpression(names)
        let hotspotsRegistry = HotspotsRegistry(solution.GetPsiServices())
        hotspotsRegistry.Register(nodes, nameExpression)

        let expr = letBindings.Bindings.[0].Expression
        IntroduceVariableResult(hotspotsRegistry, expr.As<ITreeNode>().CreateTreeElementPointer())

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
            expr.UserData.PutKey(FSharpIntroduceVariable.ExpressionToRemove)

        let workflow = IntroduceVariableWorkflow(solution, null)
        RefactoringActionUtil.ExecuteRefactoring(dataContext, workflow)

    static member CanIntroduceVar(expr: IFSharpExpression, allowInSeqExprOnly) =
        if not (isValid expr) then false else

        let isInSeqExpr (expr: IFSharpExpression) =
            let sequentialExpr = SequentialExprNavigator.GetByExpression(expr)
            if isNull sequentialExpr then false else

            let nextMeaningfulSibling = expr.GetNextMeaningfulSibling()
            nextMeaningfulSibling :? IFSharpExpression && nextMeaningfulSibling.Indent = expr.Indent

        let rec isValidExpr (expr: IFSharpExpression) =
            match expr with
            | :? IReferenceExpr as refExpr ->
                let declaredElement = refExpr.Reference.Resolve().DeclaredElement
                not (declaredElement :? ITypeElement || declaredElement :? INamespace)

            | :? IParenExpr as parenExpr ->
                isValidExpr parenExpr.InnerExpression

            | _ -> true

        let isAllowedContext (expr: IFSharpExpression) =
            let topLevelExpr = skipIntermediateParentsOfSameType<IFSharpExpression>(expr)
            if isNotNull (AttributeNavigator.GetByExpression(topLevelExpr)) then false else

            true

        if allowInSeqExprOnly && not (isInSeqExpr expr) then false else
        if not (isAllowedContext expr) then false else
        isValidExpr expr


type FSharpIntroduceVarHelper() =
    inherit IntroduceVariableHelper()

    let isExpressionToRemove (expr: ITreeNode) =
        expr.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemove)

    override x.IsLanguageSupported = true

    override x.CheckAvailability(node) =
        let expr = node.As<IFSharpExpression>()
        if isNull expr then false else

        if expr.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemove) then true else

        if not (expr.FSharpExperimentalFeaturesEnabled()) then false else
        FSharpIntroduceVariable.CanIntroduceVar(expr, false)

    override x.CheckOccurrence(expr, occurrence) =
        if isExpressionToRemove occurrence then true else

        if isExpressionToRemove expr then false else
        expr.FSharpExperimentalFeaturesEnabled()
