namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveConstructorFix(error: OnlyClassCanTakeValueArgumentsError) =
    inherit FSharpQuickFixBase()

    let ctorDecl = error.CtorDecl

    override this.Text = "Remove constructor"

    override this.IsAvailable _ =
        isValid ctorDecl

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(ctorDecl.IsPhysical())
        ModificationUtil.DeleteChild(ctorDecl)
