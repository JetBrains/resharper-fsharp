namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open JetBrains.Application.DataContext
open JetBrains.Application.UI.Actions.ActionManager
open JetBrains.Diagnostics
open JetBrains.DocumentModel.DataContext
open JetBrains.Lifetimes
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.Refactorings
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific
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

type FSharpIntroduceVariableWorkflow(solution, actionId, removeSourceExpr) =
    inherit IntroduceVariableWorkflow(solution, actionId)

    member x.RemoveSourceExpr = removeSourceExpr

type FSharpIntroduceVariable(workflow, solution, driver) =
    inherit IntroduceVariableBase(workflow, solution, driver)

    let getNames (expr: ISynExpr) =
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

    let getReplaceRanges (expr: ISynExpr) (parent: ISynExpr) =
        let sequentialExpr = SequentialExprNavigator.GetByExpression(expr)
        if expr == parent && isNotNull sequentialExpr then
            Assertion.Assert(expr == parent, "expr == parent")
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

    let rec getExprToInsertBefore (expr: ISynExpr): ISynExpr =
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

        | :? ISynExpr as parentExpr -> getExprToInsertBefore parentExpr
        | _ -> expr

    let getContextDeclaration (contextExpr: ISynExpr): IModuleMember =
        let letDecl = LetModuleDeclNavigator.GetByBinding(BindingNavigator.GetByExpression(contextExpr))
        if isNotNull letDecl then letDecl :> _ else

        let doDecl = DoNavigator.GetByExpression(contextExpr)
        if isNotNull doDecl && doDecl.IsImplicit then doDecl :> _ else null

    let createBinding (context: ISynExpr) (contextDecl: IModuleMember) name expr: ILet =
        let elementFactory = context.CreateElementFactory()
        if isNotNull contextDecl then
            elementFactory.CreateLetModuleDecl(name, expr) :> _
        else
            elementFactory.CreateLetBindingExpr(name, expr) :> _

    static member val ExpressionToRemove = Key("FSharpIntroduceVariable.ExpressionToRemove")

    override x.Process(data) =
        let initialExpr = data.SourceExpression.As<ISynExpr>()
        let commonParentExpr = data.Usages.FindLCA().As<ISynExpr>()

        // contextDecl is not null when expression is bound to a module/type let binding
        let contextExpr = getExprToInsertBefore commonParentExpr
        let contextDecl = getContextDeclaration contextExpr
        let contextIndent = if isNotNull contextDecl then contextDecl.Indent else contextExpr.Indent

        let names = getNames initialExpr
        let name = if names.Count > 0 then names.[0] else "x"

        let removeSourceExpr = initialExpr.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemove)
        initialExpr.UserData.RemoveKey(FSharpIntroduceVariable.ExpressionToRemove)

        use writeCookie = WriteLockCookie.Create(initialExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let lineEnding = initialExpr.GetLineEnding()
        let elementFactory = initialExpr.CreateElementFactory()

        let letOrUseExpr = createBinding contextExpr contextDecl name initialExpr

        let replacedUsages =
            data.Usages
            |> Array.ofSeq
            |> Array.choose (fun usage ->
                if not (isValid usage) then None else
                if removeSourceExpr && obj.ReferenceEquals(usage, contextExpr) then None else

                let ref = elementFactory.CreateReferenceExpr(name)
                Some (ModificationUtil.ReplaceChild(usage, ref).As<ITreeNode>().CreateTreeElementPointer()))

        let letBindings: ILet = 
            match letOrUseExpr with
            | :? ILetOrUseExpr ->
                let ranges = getReplaceRanges initialExpr contextExpr
                let replaced = ModificationUtil.ReplaceChildRange(ranges.ReplaceRange, TreeRange(letOrUseExpr))
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
                    letOrUseExpr
                    NewLine(lineEnding)
                    Whitespace(contextIndent)
                ] |> ignore
                letOrUseExpr

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

        let namePat = letBindings.Bindings.[0].HeadPattern
        IntroduceVariableResult(hotspotsRegistry, namePat.As<ITreeNode>().CreateTreeElementPointer())

    static member IntroduceVar(expr: ISynExpr, textControl: ITextControl, ?removeSourceExpr) =
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

        let workflow = FSharpIntroduceVariableWorkflow(solution, null, removeSourceExpr)
        RefactoringActionUtil.ExecuteRefactoring(dataContext, workflow)

    static member CanIntroduceVar(expr: ISynExpr, allowInSeqExprOnly) =
        if not (isValid expr) then false else

        let isInSeqExpr (expr: ISynExpr) =
            let sequentialExpr = SequentialExprNavigator.GetByExpression(expr)
            if isNull sequentialExpr then false else

            let nextMeaningfulSibling = expr.GetNextMeaningfulSibling()
            nextMeaningfulSibling :? ISynExpr && nextMeaningfulSibling.Indent = expr.Indent

        let rec isValidExpr (expr: ISynExpr) =
            match expr with
            | :? IReferenceExpr as refExpr ->
                let declaredElement = refExpr.Reference.Resolve().DeclaredElement
                not (declaredElement :? ITypeElement || declaredElement :? INamespace)

            | :? IParenExpr as parenExpr ->
                isValidExpr parenExpr.InnerExpression

            | _ -> true

        let isAllowedContext (expr: ISynExpr) =
            let topLevelExpr = skipIntermediateParentsOfSameType<ISynExpr>(expr)
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
        let expr = node.As<ISynExpr>()
        if isNull expr then false else

        if expr.UserData.HasKey(FSharpIntroduceVariable.ExpressionToRemove) then true else

        if not (expr.FSharpExperimentalFeaturesEnabled()) then false else
        FSharpIntroduceVariable.CanIntroduceVar(expr, false)

    override x.CheckOccurrence(expr, occurrence) =
        if isExpressionToRemove occurrence then true else

        if isExpressionToRemove expr then false else
        expr.FSharpExperimentalFeaturesEnabled()
