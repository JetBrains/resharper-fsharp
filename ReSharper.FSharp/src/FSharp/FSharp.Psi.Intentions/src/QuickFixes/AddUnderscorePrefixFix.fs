namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type AddUnderscorePrefixFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()

    let pat = warning.Pat.As<IReferencePat>()

    override x.Text = sprintf "Rename to '_%s'" pat.SourceName

    override x.IsAvailable _ =
        if not (isValid pat) || pat.SourceName = SharedImplUtil.MISSING_DECLARATION_NAME then false else

        pat.GetPartialDeclarations() |> Seq.forall (fun pat ->
            let refPat = pat :?> IReferencePat
            if isNull refPat then false else

            let identifier = refPat.Identifier
            isValid identifier && not (identifier.GetText().IsEscapedWithBackticks()))


    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())

        let patterns = pat.GetPartialDeclarations() |> Seq.toList
        for pat in patterns do
            let refPat = pat :?> IReferencePat
            refPat.SetName("_" + refPat.SourceName, ChangeNameKind.SourceName)
