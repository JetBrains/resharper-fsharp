namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
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
open JetBrains.Util

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

    let getRaplaceRanges (expr: ISynExpr) (parent: ISynExpr) =
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

    static member val Key = Key("")

    override x.Process(data) =
        let expr = data.SourceExpression.As<ISynExpr>()
        let parentExpr = data.Usages.FindLCA().As<ISynExpr>()

        let names = getNames expr
        let name = if names.Count > 0 then names.[0] else "x"

        expr.UserData.RemoveKey(FSharpIntroduceVariable.Key)
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let lineEnding = expr.GetLineEnding()
        let parentExprIndent = parentExpr.Indent

        let elementFactory = expr.CreateElementFactory()
        let letOrUseExpr = elementFactory.CreateLetBindingExpr(name, expr)

        let replacedUsages =
            data.Usages
            |> Array.ofSeq
            |> Array.choose (fun usage ->
                if not (isValid usage) || obj.ReferenceEquals(usage, parentExpr) then None else
                let ref = elementFactory.CreateReferenceExpr(name)
                Some (ModificationUtil.ReplaceChild(usage, ref).As<ITreeNode>().CreateTreeElementPointer()))

        let ranges = getRaplaceRanges expr parentExpr
        let replaced = ModificationUtil.ReplaceChildRange(ranges.ReplaceRange, TreeRange(letOrUseExpr))
        let letOrUseExpr = replaced.First :?> ILetBindings

        let binding = letOrUseExpr.Bindings.[0]
        let replaced = ModificationUtil.ReplaceChildRange(TreeRange(binding.NextSibling, letOrUseExpr.LastChild), ranges.InRange)
        if ranges.AddNewLine then
            let anchor = ModificationUtil.AddChildBefore(replaced.First, NewLine(lineEnding))
            ModificationUtil.AddChildAfter(anchor, Whitespace(parentExprIndent)) |> ignore

        let nodes =
            let replacedNodes =
                replacedUsages
                |> Array.choose (fun pointer -> pointer.GetTreeNode() |> Option.ofObj)

            [| letOrUseExpr.As<ILet>().Bindings.[0].HeadPattern :> ITreeNode |]
            |> Array.append replacedNodes 

        let nameExpression = NameSuggestionsExpression(names)
        let hotspotsRegistry = HotspotsRegistry(solution.GetPsiServices())
        hotspotsRegistry.Register(nodes, nameExpression)

        let expr = letOrUseExpr :?> ILetLikeExpr

        IntroduceVariableResult(hotspotsRegistry, expr.Bindings.[0].HeadPattern.As<ITreeNode>().CreateTreeElementPointer())


type FSharpIntroduceVarHelper() =
    inherit IntroduceVariableHelper()

    override x.IsLanguageSupported = true

    override x.CheckAvailability(node) =
        node.UserData.HasKey(FSharpIntroduceVariable.Key) ||

        // todo: change to something meaningful. :)
        node.IsSingleLine && node.GetSolution().RdFSharpModel.EnableExperimentalFeaturesSafe

    override x.CheckOccurrence(expr, occurrence) =
        // Disable getting other expressions when invoked via quick fix.
        // todo: change platform APIs in next release
        expr.GetSolution().RdFSharpModel.EnableExperimentalFeaturesSafe ||
        occurrence.UserData.HasKey(FSharpIntroduceVariable.Key)
