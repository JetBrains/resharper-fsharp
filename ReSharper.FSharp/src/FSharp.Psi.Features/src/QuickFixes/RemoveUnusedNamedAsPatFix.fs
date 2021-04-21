namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

// todo: combine with ReplaceWithWildPat:
//   match () with
//   | _ as a & _
//   | a & _ -> ()

type RemoveUnusedNamedAsPatFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()

    let pat = warning.Pat.As<IAsPat>()

    override x.Text = "Remove unused 'as' pattern"

    override x.IsAvailable _ =
        isValid pat && isNotNull pat.Pattern

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        replaceWithCopy pat pat.Pattern
