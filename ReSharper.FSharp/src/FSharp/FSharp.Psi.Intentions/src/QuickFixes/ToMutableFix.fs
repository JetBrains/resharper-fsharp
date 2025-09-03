namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI

type ToMutableFix(refExpr: IReferenceExpr) =
    inherit FSharpQuickFixBase()

    new (error: FieldNotMutableError) =
        ToMutableFix(error.RefExpr)

    new (error: ValueNotMutableError) =
        ToMutableFix(error.RefExpr)

    new (error: ValueMustBeMutableError) =
        ToMutableFix(error.RefExpr.Qualifier.As<IReferenceExpr>())

    override x.Text = $"Make '{refExpr.Identifier.GetSourceName()}' mutable"

    override x.IsAvailable _ =
        isValid refExpr &&

        let name = refExpr.Identifier.GetSourceName()
        name <> SharedImplUtil.MISSING_DECLARATION_NAME &&

        let mutableModifierOwner = refExpr.Reference.Resolve().DeclaredElement.As<IMutableModifierOwner>()
        isNotNull mutableModifierOwner &&

        mutableModifierOwner.CanBeMutable && not mutableModifierOwner.IsMutable

    override x.ExecutePsiTransaction _ =
        let element = refExpr.Reference.Resolve().DeclaredElement.As<IMutableModifierOwner>()
        element.SetIsMutable(true)
