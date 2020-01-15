namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util.PsiUtil
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithWildPatFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()

    let pat = warning.Pat.As<IReferencePat>()

    override x.Text = "Replace with '_'"

    override x.IsAvailable _ =
        isValid pat &&

        if pat.IgnoreParentParens().Parent :? IAttribPat then false else

        let node = skipIntermediatePatParents pat |> getParent
        node :? IBinding ||
        node :? IMatchClause ||
        node :? IMemberParamDeclaration && node.Parent :? IMemberDeclaration // todo: check this check

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        replace pat (pat.FSharpLanguageService.CreateElementFactory(pat.GetPsiModule()).CreateWildPat())
