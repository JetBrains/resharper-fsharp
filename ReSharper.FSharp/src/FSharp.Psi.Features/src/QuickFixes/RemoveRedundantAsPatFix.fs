namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantAsPatFix(warning: RedundantAsPatternWarning) =
    inherit FSharpQuickFixBase()

    let asPat = warning.AsPat

    override x.Text = "Remove redundant 'as' pattern"

    override x.IsAvailable _ =
        isValid asPat && isNotNull asPat.NameIdentifier

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(asPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let elementFactory = asPat.CreateElementFactory()
        let refPat = elementFactory.CreatePattern(asPat.SourceName, not asPat.IsLocal)
        replace asPat refPat
