namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type AddSetterFix(error: PropertyCannotBeSetError) =
    inherit FSharpQuickFixBase()

    let refExpr = error.RefExpr
    let mutable declaration: IAutoPropertyDeclaration = null

    override this.IsAvailable _ =
        isValid refExpr &&

        let declaredElement = refExpr.Reference.Resolve().DeclaredElement
        let decl = declaredElement.GetDeclarations() |> Seq.tryExactlyOne

        match decl with
        | None -> false
        | Some decl ->

        match decl with
        | :? IAutoPropertyDeclaration as decl ->
            declaration <- decl
            true
        | _ -> false

    override this.Text = $"Add setter to '{refExpr.ShortName}'"

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let factory = declaration.CreateElementFactory()
        let accessors = factory.CreateAccessorsNamesClause(true, true)

        declaration.SetAccessorsClause(accessors) |> ignore
        addNodeBefore declaration.AccessorsClause (Whitespace(1))
