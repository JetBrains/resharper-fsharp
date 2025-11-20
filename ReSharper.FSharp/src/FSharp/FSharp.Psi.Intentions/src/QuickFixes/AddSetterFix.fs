namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type AddSetterFix(error: PropertyCannotBeSetError) =
    inherit FSharpQuickFixBase()

    let refExpr = error.RefExpr
    let mutable declaration: IOverridableMemberDeclaration = null

    let setterNodes setter: ITreeNode list =
        [ FSharpTokenType.COMMA.CreateLeafElement(); setter ]

    let getSetNodes getter setter: ITreeNode list =
        [ FSharpTokenType.WITH.CreateLeafElement(); getter ] @ setterNodes setter

    override this.IsAvailable _ =
        isValid refExpr &&

        let declaredElement = refExpr.Reference.Resolve().DeclaredElement
        declaredElement :? IFSharpProperty &&

        let decl = declaredElement.GetDeclarations() |> Seq.tryExactlyOne
        match decl with
        | None -> false
        | Some decl ->

        declaration <-
            match decl with
            | :? IAbstractMemberDeclaration as decl -> decl :> IOverridableMemberDeclaration
            | :? IAutoPropertyDeclaration as decl when not decl.IsVirtual -> decl
            | _ -> null

        isNotNull declaration &&

        let accessors = declaration.AccessorDeclarations
        accessors.All _.IsImplicit

    override this.Text = $"Add setter to '{refExpr.ShortName}'"

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())

        let factory = refExpr.CreateElementFactory()
        let createGetter = isNull (declaration.GetAccessor(AccessorKind.GETTER))
        let autoProperty = factory.CreateAutoPropertyDeclaration(createGetter, true)
        let accessors = autoProperty.AccessorDeclarationsEnumerable |> Seq.toList

        let nodesToAdd =
            match accessors with
            | [getter; setter] -> getSetNodes getter setter
            | [setter] -> setterNodes setter
            | _ -> []

        addNodesAfter declaration.LastChild nodesToAdd |> ignore
