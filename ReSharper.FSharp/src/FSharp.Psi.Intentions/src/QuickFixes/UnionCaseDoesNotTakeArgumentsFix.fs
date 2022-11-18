namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpModulesUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.Tree

type UnionCaseDoesNotTakeArgumentsFix(node: IFSharpPattern) =
    inherit FSharpQuickFixBase()

    let pattern =
        match node with
        | :? IParametersOwnerPat as pat when not pat.Parameters.IsEmpty -> Some pat
        | _ -> None

    new (error: UnionCaseDoesNotTakeArgumentsError) = UnionCaseDoesNotTakeArgumentsFix(error.Pattern)

    override x.Text = "This union case does not take arguments"

    override x.IsAvailable _ =
        isValid node && pattern.IsSome

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(node.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        match pattern with
        | None -> ()
        | Some pat ->
            let factory = pat.CreateElementFactory()
            let newPat = factory.CreatePattern(pat.Identifier.Name, true)
            PsiModificationUtil.replace pat newPat
