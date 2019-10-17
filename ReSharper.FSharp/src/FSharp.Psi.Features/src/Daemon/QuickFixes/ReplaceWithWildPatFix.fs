namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util.PsiUtil
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithWildPatFix(warning: UnusedValueWarning) =
    inherit QuickFixBase()

    let pat = warning.Pat.As<IReferencePat>()

    override x.Text = "Replace with '_'"

    override x.IsAvailable _ =
        isValid pat &&

        let node = skipIntermediatePatParents pat |> getParent
        node :? IBinding ||
        node :? IMatchClause ||
        node :? IMemberParamDeclaration && node.Parent :? IMemberDeclaration || // todo: check this check
        node :? ILetOrUseBangExpr

    override x.ExecutePsiTransaction(_, _) =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        replace pat (pat.FSharpLanguageService.CreateElementFactory(pat.GetPsiModule()).CreateWildPat())
        null
