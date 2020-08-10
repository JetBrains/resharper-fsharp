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

    let pat = pat.As<IReferencePat>()

    let replaceWithWildPat (pat: IFSharpPattern) =
        replace pat (pat.GetFSharpLanguageService().CreateElementFactory(pat.GetPsiModule()).CreateWildPat())

    let getPatOwner (pat: INamedPat) =
        if isNull pat then None else

        let pat = pat.IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(pat)) then None else

        let typedPat = TypedPatNavigator.GetByPattern(pat).IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(typedPat)) then None else

        Some(skipIntermediatePatParents pat)

    let isAvailable (pat: IReferencePat) =
        isValid pat &&

        match getPatOwner pat with
        | None -> false
        | Some node ->

        match node.Parent with
        | :? IBinding
        | :? IMatchClause
        | :? ILambdaParametersList -> true
        | :? IParametersPatternDeclaration as parent when
            (parent.Parent :? IMemberDeclaration || parent.Parent :? IMemberConstructorDeclaration) -> true
        | _ -> false

    let patOwner = getPatOwner pat

    new (warning: UnusedValueWarning) =
        ReplaceWithWildPatFix(warning.Pat)

    new (error: VarBoundTwiceError) =
        ReplaceWithWildPatFix(error.Pat)

    override x.Text = "Replace with '_'"

    override x.IsAvailable _ = isAvailable pat

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        replaceWithWildPat pat

    interface IHighlightingsSetScopedAction with
        member x.ScopedText = "Replace unused values with '_'"
        member x.FileCollectorInfo =
            match patOwner with
            | None -> FileCollectorInfo.Default
            | Some pat ->

            let scopeText =
                match pat.Parent with
                | :? IMatchClause ->
                    let patternText = 
                        match pat with
                        | :? ILocalParametersOwnerPat as owner -> owner.SourceName
                        | _ -> "match clause"
                    sprintf "'%s' pattern" patternText
                | :? ILambdaParametersList
                | :? IParametersPatternDeclaration -> "parameter list"
                | :? IBinding -> "binding patterns"
                | _ -> invalidArg "patOwner.Parent" "unexpected type"

            let scopeNode = if pat.Parent :? ILambdaParametersList then pat.Parent else pat :>_
            FileCollectorInfo.WithThisAndContainingLocalScopes(LocalScope(scopeNode, scopeText, scopeText))

        member x.ExecuteAction(highlightingInfos, _, _) =
            use writeLock = WriteLockCookie.Create(true)
            use disableFormatter = new DisableCodeFormatter()

            for highlightingInfo in highlightingInfos do
                let warning = highlightingInfo.Highlighting.As<UnusedValueWarning>()
                if isNull warning then () else

                let pat = warning.Pat.As<IReferencePat>()
                if isNotNull pat && isAvailable pat then
                    replaceWithWildPat pat

            null
