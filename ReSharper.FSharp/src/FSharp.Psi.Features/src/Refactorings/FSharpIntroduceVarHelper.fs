namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Naming.Elements
open JetBrains.ReSharper.Psi.Naming.Extentions
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Naming.Settings
open JetBrains.ReSharper.Psi.Pointers
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Refactorings.IntroduceVariable
open JetBrains.ReSharper.Refactorings.Workflow
open JetBrains.ReSharper.Resources.Shell

type FSharpIntroduceVariable(workflow, solution, driver) =
    inherit IntroduceVariableBase(workflow, solution, driver)

    override x.Process(data) =
        let expr = data.SourceExpression.As<ISynExpr>()

        match data.Usages.FindLCA().As<ISynExpr>() with
        | null -> null
        | parentExpr ->

        let lineEnding = expr.GetLineEnding()
        let elementFactory = expr.CreateElementFactory()

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
        let names = namesCollection.Prepare(namingRule, ScopeKind.Common, suggestionOptions).AllNames()
        let name = if names.Count > 0 then names.[0] else "x"
        
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let replacedUsages =
            data.Usages
            |> Array.ofSeq
            |> Array.map (fun usage ->
                let ref = elementFactory.CreateReferenceExpr(name)
                ModificationUtil.ReplaceChild(usage, ref).As<ITreeNode>().CreateTreeElementPointer())

        let letOrUseExpr = elementFactory.CreateLetBindingExpr(name, expr)

        letOrUseExpr.SetInExpression(parentExpr) |> ignore
        let anchor = ModificationUtil.AddChildAfter(letOrUseExpr.Bindings.[0], NewLine(lineEnding))
        ModificationUtil.AddChildAfter(anchor, Whitespace(parentExpr.Indent)) |> ignore

        let letOrUseExpr = ModificationUtil.ReplaceChild(parentExpr, letOrUseExpr) :> ITreeNode

        let nodes =
            let replacedNodes =
                replacedUsages |> Array.map (fun pointer -> pointer.GetTreeNode())

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
        node.IsSingleLine // todo: change to something meaningful. :)
