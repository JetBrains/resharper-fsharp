namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Actions
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Scopes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithWildPatFix(pat: INamedPat) =
    inherit FSharpQuickFixBase()

    let [<Literal>] fixText = "Replace with '_'"
    let pat = pat.As<IReferencePat>()

    let replaceWithWildPat (pat: IReferencePat) =
        replace pat (pat.GetFSharpLanguageService().CreateElementFactory(pat.GetPsiModule()).CreateWildPat())

    let getPatOwner (pat: INamedPat) =
        let pat = pat.IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(pat)) then None else

        let typedPat = TypedPatNavigator.GetByPattern(pat).IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(typedPat)) then None else

        Some(skipIntermediatePatParents pat |> getParent)
        
    let isAvailable (pat: IReferencePat) =
        isValid pat &&
        let owner = getPatOwner pat
        match owner with
        | None -> false
        | Some node ->
            node :? IBinding ||
            node :? IMatchClause ||
            node :? ILambdaExpr ||
            node :? IMemberParamsDeclaration &&
            (node.Parent :? IMemberDeclaration || node.Parent :? IMemberConstructorDeclaration)

    let patOwner = getPatOwner pat

    new (warning: UnusedValueWarning) =
        ReplaceWithWildPatFix(warning.Pat)

    new (error: VarBoundTwiceError) =
        ReplaceWithWildPatFix(error.Pat)

    override x.Text = fixText

    override x.IsAvailable _ = isAvailable pat

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        replaceWithWildPat pat

    interface IHighlightingsSetScopedAction with
        member x.ScopedText = fixText
        member x.FileCollectorInfo =
            match patOwner with
            | None -> FileCollectorInfo.Default
            | Some node -> 
                FileCollectorInfo.WithThisAndContainingLocalScopes(LocalScope(node, "signature", "signature"))

        member x.ExecuteAction(highlightingInfos, _, _) =
            use writeLock = WriteLockCookie.Create(true)
            use disableFormatter = new DisableCodeFormatter()

            for highlightingInfo in highlightingInfos do
                match highlightingInfo.Highlighting.As<UnusedValueWarning>() with
                | null -> ()
                | warning ->
                    let pat = warning.Pat.As<IReferencePat>()
                    let isAvailable =
                        match pat with
                        | null -> false
                        | pat -> isAvailable pat
                    if isAvailable then replaceWithWildPat pat
            null
