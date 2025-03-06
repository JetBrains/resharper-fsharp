namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Resources.Shell

type ToStaticMemberFix(error: InstanceMemberRequiresTargetError) =
    inherit FSharpQuickFixBase()

    let memberDecl = error.MemberDecl

    override this.Text = "To static"

    override this.IsAvailable _ =
        isValid memberDecl && getTokenType memberDecl.MemberKeyword == FSharpTokenType.MEMBER

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(memberDecl.IsPhysical())

        memberDecl.SetStatic(true)
