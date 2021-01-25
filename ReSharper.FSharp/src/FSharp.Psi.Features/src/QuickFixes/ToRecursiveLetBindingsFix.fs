namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

type ToRecursiveLetBindingsFix(error: LetAndForNonRecBindingsError) =
    inherit FSharpQuickFixBase()

    let letBindings = error.LetBindings
    
    override x.Text = "To recursive"
    override x.IsAvailable _ = isValid letBindings

    override x.ExecutePsiTransaction _ =
        use cookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        letBindings.SetIsRecursive(true)
