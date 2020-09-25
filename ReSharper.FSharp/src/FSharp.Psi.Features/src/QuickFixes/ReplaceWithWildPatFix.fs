namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Actions
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.QuickFixes
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Scopes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

module ReplaceWithWildPat =
    let replaceWithWildPat (pat: IFSharpPattern) =
        if isIdentifierOrKeyword (pat.GetPreviousToken()) then
            ModificationUtil.AddChildBefore(pat, Whitespace()) |> ignore

        if isIdentifierOrKeyword (pat.GetNextToken()) then
            ModificationUtil.AddChildAfter(pat, Whitespace()) |> ignore

        replace pat (pat.GetFSharpLanguageService().CreateElementFactory(pat.GetPsiModule()).CreateWildPat())

    let getPatOwner (pat: IFSharpPattern) =
        if isNull pat then None else

        let pat = pat.IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(pat)) then None else

        let typedPat = TypedPatNavigator.GetByPattern(pat).IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(typedPat)) then None else

        Some(skipIntermediatePatParents pat)

    let isAvailable (pat: IFSharpPattern) =
        isValid pat && not (pat :? IParametersOwnerPat) &&

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


type ReplaceWithWildPatScopedFix(pat: IFSharpPattern, highlightingType) =
    inherit FSharpScopedQuickFixBase()

    new (warning: RedundantUnionCaseFieldPatternsWarning) =
        ReplaceWithWildPatScopedFix(warning.ParenPat, warning.GetType())

    new (warning: RedundantParenPatWarning) =
        ReplaceWithWildPatScopedFix(warning.ParenPat, warning.GetType())

    override x.Text = "Replace with '_'"
    override x.TryGetContextTreeNode() = pat :> _

    override x.GetScopedFixingStrategy(solution) =
        SameQuickFixSameHighlightingTypeStrategy(highlightingType, x, solution) :> _

    override x.IsAvailable _ =
        ReplaceWithWildPat.isAvailable pat

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        ReplaceWithWildPat.replaceWithWildPat pat


type ReplaceWithWildPatFix(pat: IFSharpPattern, isFromUnusedValue) =
    inherit FSharpQuickFixBase()

    let patOwner = ReplaceWithWildPat.getPatOwner pat

    new (warning: UnusedValueWarning) =
        ReplaceWithWildPatFix(warning.Pat, true)

    new (error: VarBoundTwiceError) =
        ReplaceWithWildPatFix(error.Pat, false)

    override x.Text = "Replace with '_'"

    override x.IsAvailable _ = ReplaceWithWildPat.isAvailable pat

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        ReplaceWithWildPat.replaceWithWildPat pat

    interface IHighlightingsSetScopedAction with
        member x.ScopedText = "Replace unused values with '_'"

        member x.FileCollectorInfo =
            if not isFromUnusedValue then FileCollectorInfo.Empty else

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
            FileCollectorInfo.WithLocalAndAdditionalScopes(scopeNode, LocalScope(scopeNode, scopeText, scopeText))

        member x.ExecuteAction(highlightingInfos, _, _) =
            use writeLock = WriteLockCookie.Create(true)
            use disableFormatter = new DisableCodeFormatter()

            for highlightingInfo in highlightingInfos do
                let warning = highlightingInfo.Highlighting.As<UnusedValueWarning>()
                if isNotNull warning && ReplaceWithWildPat.isAvailable warning.Pat then
                    ReplaceWithWildPat.replaceWithWildPat warning.Pat

            null
