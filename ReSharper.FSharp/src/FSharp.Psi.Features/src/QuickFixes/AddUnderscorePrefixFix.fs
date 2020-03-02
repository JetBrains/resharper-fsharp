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
        if not (isValid pat) then false else

        let identifier = pat.Identifier
        if not (isValid identifier) || identifier.GetText().IsEscapedWithBackticks() then false else

        pat.SourceName <> SharedImplUtil.MISSING_DECLARATION_NAME

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        pat.SetName("_" + pat.SourceName, ChangeNameKind.SourceName)
