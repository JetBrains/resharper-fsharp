namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI

type ToRecursiveLetBindingsFix(error: LetAndForNonRecBindingsError) =
    inherit FSharpQuickFixBase()

    let letBindings = error.LetBindings

    override x.Text =
        let bindings = letBindings.BindingsEnumerable

        let name =
            bindings
            |> Seq.tryHead
            |> Option.bind (fun binding ->
                match binding.GetHeadPatternName() with
                | SharedImplUtil.MISSING_DECLARATION_NAME -> None
                | name -> Some $"'{name}'")
            |> Option.defaultValue "bindings"

        $"Make {name} recursive"

    override x.IsAvailable _ =
        isValid letBindings &&

        letBindings.BindingsEnumerable
        |> Seq.tryHead
        |> Option.forall (fun binding -> binding.HeadPattern.IgnoreInnerParens() :? IReferencePat)

    override x.ExecutePsiTransaction _ =
        ToRecursiveLetBindingsAction.Execute(letBindings)
