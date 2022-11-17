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
    let mutable declaration: IAccessorsNamesClauseOwner = null

    override this.IsAvailable _ =
        isValid refExpr &&

        let declaredElement = refExpr.Reference.Resolve().DeclaredElement
        let decl = declaredElement.GetDeclarations() |> Seq.tryExactlyOne

        declaredElement :? IFSharpProperty &&

        match decl with
        | None -> false
        | Some decl ->

        declaration <-
            match decl with
            | :? IAbstractMemberDeclaration as decl -> decl :> IAccessorsNamesClauseOwner
            | :? IAutoPropertyDeclaration as decl when not decl.IsVirtual -> decl
            | _ -> null

        isNotNull declaration

    override this.Text = $"Add setter to '{refExpr.ShortName}'"

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let factory = refExpr.CreateElementFactory()
        let accessors = factory.CreateAccessorsNamesClause(true, true)

        declaration.SetAccessorsClause(accessors) |> ignore
        if not (declaration.AccessorsClause.PrevSibling :? Whitespace) then
            addNodeBefore declaration.AccessorsClause (Whitespace(1))
