namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi

type ReplaceXmlDocWithLineCommentFix(warning: InvalidXmlDocPositionWarning) =
    inherit FSharpScopedQuickFixBase(warning.Comment)

    let comment = warning.Comment

    override this.IsAvailable _ = comment.IsValid()
    override x.Text = "Replace XML comment with line comment"

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(comment.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        replace comment (FSharpComment.CreateLineComment(comment.CommentText))


type RemoveXmlDocFix(warning: InvalidXmlDocPositionWarning) =
    inherit FSharpQuickFixBase()

    let comment = warning.Comment
    
    override this.IsAvailable _ = comment.IsValid()
    override this.Text = "Remove XML comment"

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(comment.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        deleteChild comment
