namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
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
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let replacedUsages =
            data.Usages
            |> Array.ofSeq
            |> Array.map (fun usage ->
                let ref = elementFactory.CreateReferenceExpr("x")
                ModificationUtil.ReplaceChild(usage, ref).As<ITreeNode>().CreateTreeElementPointer())

        let letOrUseExpr = elementFactory.CreateLetBindingExpr("x", expr)

        letOrUseExpr.SetInExpression(parentExpr) |> ignore
        let anchor = ModificationUtil.AddChildAfter(letOrUseExpr.Bindings.[0], NewLine(lineEnding))
        ModificationUtil.AddChildAfter(anchor, Whitespace(parentExpr.Indent)) |> ignore

        let letOrUseExpr = ModificationUtil.ReplaceChild(parentExpr, letOrUseExpr) :> ITreeNode

        let nodes =
            replacedUsages
            |> Array.map (fun pointer -> pointer.GetTreeNode())
            |> Array.append [| letOrUseExpr.As<ILet>().Bindings.[0].HeadPattern |]
        
        let nameExpression = NameSuggestionsExpression(["xxx"; "yyy"])
        let hotspotsRegistry = HotspotsRegistry(solution.GetPsiServices())
        hotspotsRegistry.Register(nodes, nameExpression)
        
        IntroduceVariableResult(hotspotsRegistry, letOrUseExpr.CreateTreeElementPointer())


type FSharpIntroduceVarHelper() =
    inherit IntroduceVariableHelper()

    override x.IsLanguageSupported = true

    override x.CheckAvailability(node) =
        node.IsSingleLine // todo: change to something meaningful. :)


[<Language(typeof<FSharpLanguage>)>]
type FSharpRefactoringLanguageService() =
    inherit InternalRefactoringLanguageService()

    override x.CreateIntroduceVariableHelper() = FSharpIntroduceVarHelper() :> _

    override x.CreateIntroduceVariable(workflow, solution, driver) =
        FSharpIntroduceVariable(workflow, solution, driver) :> _
