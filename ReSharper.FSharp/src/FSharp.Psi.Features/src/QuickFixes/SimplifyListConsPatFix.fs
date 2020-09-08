namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type SimplifyListConsPatFix(warning: ConsWithEmptyListPatWarning) =
    inherit FSharpQuickFixBase()

    let pat = warning.ListConsPat

    override x.Text = "Simplify pattern"

    override x.IsAvailable _ =
        isValid pat && isValid pat.HeadPattern &&

        let tailPat = pat.TailPattern.As<IListPat>()
        isValid tailPat && isNotNull tailPat.LeftBracket

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())

        let listPat = pat.TailPattern :?> IListPat

        match listPat.LeftBracket.NextSibling with
        | :? Whitespace as ws -> ModificationUtil.DeleteChild(ws)
        | _ -> ()

        ModificationUtil.AddChildAfter(listPat.LeftBracket, pat.HeadPattern) |> ignore
        ModificationUtil.AddChildBefore(pat, listPat) |> ignore
        ModificationUtil.DeleteChild(pat)
