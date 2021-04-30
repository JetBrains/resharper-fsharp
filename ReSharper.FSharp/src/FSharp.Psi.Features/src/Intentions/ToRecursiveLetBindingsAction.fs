namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

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

    static member Execute(letBindings: ILetBindings) =
        use writeCookie = WriteLockCookie.Create(letBindings.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        letBindings.SetIsRecursive(true)

        letBindings.BindingsEnumerable
        |> Seq.tryHead
        |> Option.iter (fun binding ->
            let expr = binding.Expression
            if isNotNull expr && expr.StartLine = letBindings.StartLine then
                shiftNode 4 binding.Expression)

    
    override x.ExecutePsiTransaction(_, _) =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        ToRecursiveLetBindingsAction.Execute(letBindings)

        null
