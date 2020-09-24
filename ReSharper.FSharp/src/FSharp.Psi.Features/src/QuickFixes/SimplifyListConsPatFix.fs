namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
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

        let settings = pat.GetSettingsStoreWithEditorConfig()
        let addSpaces = settings.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceAroundDelimiter)

        let listPat = pat.TailPattern :?> IListPat

        match listPat.LeftBracket.NextSibling with
        | :? Whitespace as ws -> ModificationUtil.DeleteChild(ws)
        | _ -> ()

        let innerListPat = ModificationUtil.AddChildAfter(listPat.LeftBracket, pat.HeadPattern)

        let nextSibling = innerListPat.NextSibling
        if nextSibling != listPat.RightBracket && not (nextSibling :? NewLine) then
            ModificationUtil.AddChildAfter(innerListPat, Whitespace()) |> ignore

        let rightBracketPrevSibling = listPat.RightBracket.PrevSibling

        if addSpaces then
            ModificationUtil.AddChildBefore(innerListPat, Whitespace()) |> ignore
            if not (rightBracketPrevSibling :? Whitespace) && not (rightBracketPrevSibling :? NewLine) then
                ModificationUtil.AddChildBefore(listPat.RightBracket, Whitespace()) |> ignore
        else
            if rightBracketPrevSibling :? Whitespace then
                ModificationUtil.DeleteChild(rightBracketPrevSibling)

        ModificationUtil.AddChildBefore(pat, listPat) |> ignore
        ModificationUtil.DeleteChild(pat)
