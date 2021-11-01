namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type ToMutableFix(refExpr: IReferenceExpr) =
    inherit FSharpQuickFixBase()

    new (error: FieldNotMutableError) =
        ToMutableFix(error.RefExpr)

    new (error: ValueNotMutableError) =
        ToMutableFix(error.RefExpr)

    override x.Text = "Make " + refExpr.Identifier.GetSourceName() + " mutable"

    override x.IsAvailable _ =
        if not (isValid refExpr) then false else

        let mutableModifierOwner = refExpr.Reference.Resolve().DeclaredElement.As<IMutableModifierOwner>()
        if isNull mutableModifierOwner then false else

        mutableModifierOwner.CanBeMutable && not mutableModifierOwner.IsMutable

    override x.ExecutePsiTransaction _ =
        let element = refExpr.Reference.Resolve().DeclaredElement.As<IMutableModifierOwner>()
        element.SetIsMutable(true)
