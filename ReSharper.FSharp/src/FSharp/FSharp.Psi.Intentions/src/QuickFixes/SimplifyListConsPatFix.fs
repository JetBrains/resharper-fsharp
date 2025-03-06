namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type SimplifyListConsPatFix(warning: ConsWithEmptyListPatWarning) =
    inherit FSharpQuickFixBase()

    let pat = warning.ListConsPat

    override x.Text = "Simplify pattern"

    override x.IsAvailable _ =
        isValid pat && isValid pat.HeadPattern &&

        let listPat = pat.TailPattern.As<IListPat>()
        isValid listPat && isNotNull listPat.LeftBracket && isNotNull listPat.RightBracket

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())

        let listPat = pat.TailPattern :?> IListPat
        ModificationUtil.AddChildAfter(listPat.LeftBracket, pat.HeadPattern) |> ignore
        replaceWithCopy pat listPat
