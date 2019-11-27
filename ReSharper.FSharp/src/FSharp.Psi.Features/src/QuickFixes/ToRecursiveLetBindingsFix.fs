namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type ToRecursiveLetBindingsFix(error: LetAndForNonRecBindingsError) =
    inherit FSharpQuickFixBase()

    let letBindings = error.LetBindings
    
    override x.Text = "To recursive"
    override x.IsAvailable _ = isValid letBindings

    override x.ExecutePsiTransaction _ =
        letBindings.SetIsRecursive(true)
