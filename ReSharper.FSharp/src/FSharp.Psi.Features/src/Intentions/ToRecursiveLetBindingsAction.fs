namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ContextAction(Name = "ToRecursiveLetBindings", Group = "F#", Description = "To recursive")>]
type ToRecursiveLetBindingsAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "To recursive"

    override x.IsAvailable _ =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        if isNull letBindings || letBindings.IsRecursive then false else
        if not (isAtLetExprKeywordOrNamedPat dataProvider letBindings) then false else

        let bindings = letBindings.Bindings
        bindings.Count = 1 && bindings.[0].HasParameters

    override x.ExecutePsiTransaction(_, _) =
        use cookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        letBindings.SetIsRecursive(true)

        null
