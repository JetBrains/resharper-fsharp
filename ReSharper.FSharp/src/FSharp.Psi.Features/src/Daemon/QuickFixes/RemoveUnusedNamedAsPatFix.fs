namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util.PsiUtil
open JetBrains.ReSharper.Resources.Shell

type RemoveUnusedNamedAsPatFix(warning: UnusedValueWarning) =
    inherit QuickFixBase()

    let pat = warning.Pat.As<IAsPat>()

    override x.Text = "Remove unused pattern"

    override x.IsAvailable _ =
        isValid pat && isNotNull pat.Pattern

    override x.ExecutePsiTransaction(_, _) =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        replaceWithCopy pat pat.Pattern
        null
