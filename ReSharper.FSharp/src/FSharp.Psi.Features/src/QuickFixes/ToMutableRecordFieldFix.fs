namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type ToMutableRecordFieldFix(error: FieldNotMutableError) =
    inherit FSharpQuickFixBase()

    let refExpr = error.RefExpr
    
    override x.Text = "Make " + refExpr.Identifier.GetSourceName() + " mutable"

    override x.IsAvailable _ =
        if not (isValid refExpr) then false else

        let element = refExpr.Reference.Resolve().DeclaredElement
        element :? IRecordField

    override x.ExecutePsiTransaction _ =
        let element = refExpr.Reference.Resolve().DeclaredElement
        for decl in element.GetDeclarations() do
            let fieldDecl = decl.As<IRecordFieldDeclaration>()
            if isNotNull fieldDecl then
                fieldDecl.SetIsMutable(true)
