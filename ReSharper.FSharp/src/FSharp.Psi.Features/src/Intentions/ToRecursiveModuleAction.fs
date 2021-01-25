namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ContextAction(Name = "ToRecursiveModule", Group = "F#", Description = "To recursive")>]
type ToRecursiveModuleAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "To recursive"

    override x.IsAvailable _ =
        let moduleDeclaration = dataProvider.GetSelectedElement<IDeclaredModuleLikeDeclaration>()
        if isNull moduleDeclaration || moduleDeclaration.IsRecursive then false else

        isAtModuleDeclarationKeyword dataProvider moduleDeclaration

    override x.ExecutePsiTransaction(_, _) =
        use cookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        let moduleLikeDeclaration = dataProvider.GetSelectedElement<IDeclaredModuleLikeDeclaration>()
        moduleLikeDeclaration.SetIsRecursive(true)

        null
