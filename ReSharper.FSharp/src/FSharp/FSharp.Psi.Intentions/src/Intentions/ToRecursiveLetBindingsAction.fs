namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "ToRecursiveLetBindings", GroupType = typeof<FSharpContextActions>, Description = "To recursive")>]
type ToRecursiveLetBindingsAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "To recursive"

    override x.IsAvailable _ =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        if isNull letBindings || letBindings.IsRecursive then false else
        if not (isAtLetExprKeywordOrReferencePattern dataProvider letBindings) then false else

        let bindings = letBindings.Bindings
        bindings.Count = 1 && bindings[0].HasParameters

    static member Execute(letBindings: ILetBindings) =
        use writeCookie = WriteLockCookie.Create(letBindings.IsPhysical())
        letBindings.SetIsRecursive(true)

    override x.ExecutePsiTransaction(_, _) =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        ToRecursiveLetBindingsAction.Execute(letBindings)

        null
