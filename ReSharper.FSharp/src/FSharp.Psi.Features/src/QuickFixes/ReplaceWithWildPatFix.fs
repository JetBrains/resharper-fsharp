namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithWildPatFix(pat: INamedPat) =
    inherit FSharpQuickFixBase()

    let pat = pat.As<IReferencePat>()

    new (warning: UnusedValueWarning) =
        ReplaceWithWildPatFix(warning.Pat)

    new (error: VarBoundTwiceError) =
        ReplaceWithWildPatFix(error.Pat)

    override x.Text = "Replace with '_'"

    override x.IsAvailable _ =
        isValid pat &&

        let pat = pat.IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(pat)) then false else

        let typedPat = TypedPatNavigator.GetByPattern(pat).IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(typedPat)) then false else

        let node = skipIntermediatePatParents pat |> getParent
        node :? IBinding ||
        node :? IMatchClause ||
        node :? ILambdaExpr ||
        node :? IMemberParamsDeclaration &&
                (node.Parent :? IMemberDeclaration || node.Parent :? IMemberConstructorDeclaration)

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        replace pat (pat.GetFSharpLanguageService().CreateElementFactory(pat.GetPsiModule()).CreateWildPat())
