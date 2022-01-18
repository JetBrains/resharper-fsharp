namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantAsPatFix(warning: RedundantAsPatternWarning) =
    inherit FSharpQuickFixBase()

    let asPat = warning.AsPat

    override x.Text = "Remove redundant 'as' pattern"

    override x.IsAvailable _ =
        isValid asPat &&
        
        let refPat = asPat.RightPattern.As<IReferencePat>()
        isNotNull refPat && isNotNull refPat.NameIdentifier

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(asPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let refPat = asPat.RightPattern :?> IReferencePat
        let elementFactory = asPat.CreateElementFactory()
        let refPat = elementFactory.CreatePattern(refPat.SourceName, not refPat.IsLocal)
        replace asPat refPat
