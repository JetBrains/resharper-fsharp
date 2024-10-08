namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Resources.Shell

// todo: use ScopedImportQuickFixBase + ModernBulbActionBase

type FSharpImportExtensionMemberFix(reference: IReference) =
    inherit FSharpQuickFixBase()

    let findExtensionMembers () : seq<ITypeMember> =
        if isNull reference then [] else

        let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
        if isNull refExpr then [] else

        let qualifierExpr = refExpr.Qualifier
        if isNull qualifierExpr then [] else

        let fcsType = qualifierExpr.TryGetFcsType()
        if isNull fcsType then [] else

        let name = reference.GetName()
        FSharpExtensionMemberUtil.getExtensionMembers qualifierExpr fcsType (Some name) |> Seq.cast

    override this.Text =
        let typeMember = findExtensionMembers () |> Seq.head
        let containingTypeShortName = typeMember.ContainingType.ShortName
        $"Use {containingTypeShortName}.{reference.GetName()}"

    override this.IsAvailable _ =
        findExtensionMembers ()
        |> Seq.isEmpty
        |> not

    override this.ExecutePsiTransaction _ =
        let reference = reference :?> FSharpSymbolReference
        let referenceOwner = reference.GetElement()
        let typeMember = findExtensionMembers () |> Seq.head

        use writeCookie = WriteLockCookie.Create(referenceOwner.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        FSharpBindUtil.bindDeclaredElementToReference referenceOwner reference typeMember "bind"
