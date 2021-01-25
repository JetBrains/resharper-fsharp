namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Actions
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Scopes
open JetBrains.ReSharper.Feature.Services.QuickFixes.Scoped
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
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

        for pat in pat.GetPartialDeclarations() do
            replace pat (pat.GetFSharpLanguageService().CreateElementFactory(pat.GetPsiModule()).CreateWildPat())

    let getPatOwner (pat: IFSharpPattern) =
        if isNull pat then null else

        let pat = pat.IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(pat)) then null else
        if isNotNull (OptionalValPatNavigator.GetByPattern(pat)) then null else

        let typedPat = TypedPatNavigator.GetByPattern(pat).IgnoreParentParens()
        if isNotNull (AttribPatNavigator.GetByPattern(typedPat)) then null else

        skipIntermediatePatParents pat

    let isAvailable (pat: IFSharpPattern) =
        isValid pat &&

        let binding = BindingNavigator.GetByHeadPattern(pat)
        if isNotNull binding && binding.HasParameters then false else

        match pat with
        | :? IParametersOwnerPat
        | :? IAsPat -> false // todo: allow for 'as' patterns, check if inner patterns are used
        | _ ->

        let node = getPatOwner pat
        if isNull node then false else

        match node.Parent with
        | :? IBinding | :? IMatchClause | :? ILambdaParametersList -> true

        | :? IParametersPatternDeclaration as parent ->
            let parent = parent.Parent
            parent :? IMemberDeclaration || parent :? ISecondaryConstructorDeclaration || parent :? IBinding

        | _ -> false


type ReplaceWithWildPatScopedFix(pat: IFSharpPattern, highlightingType) =
    inherit FSharpScopedQuickFixBase()

    new (warning: RedundantUnionCaseFieldPatternsWarning) =
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
            | null -> FileCollectorInfo.Default
            | pat ->

            let (scopeNode: ITreeNode), scopeText =
                match pat.Parent with
                | :? IMatchClause ->
                    let patternText = 
                        match pat with
                        | :? IParametersOwnerPat as owner -> owner.ReferenceName.ShortName
                        | _ -> "match clause"
                    pat :> _, sprintf "'%s' pattern" patternText

                | :? IParametersPatternDeclaration as p ->
                    match BindingNavigator.GetByParametersPattern(p) with
                    | null -> pat :> _, "parameter list"
                    | binding -> binding :> _, "binding patterns"

                | :? ILambdaParametersList as parametersList -> parametersList :> _, "parameter list"
                | :? IBinding -> pat :> _, "binding patterns"
                | _ -> invalidArg "patOwner.Parent" "unexpected type"

            FileCollectorInfo.WithLocalAndAdditionalScopes(scopeNode, LocalScope(scopeNode, scopeText, scopeText))

        member x.ExecuteAction(highlightingInfos, _, _) =
            use writeLock = WriteLockCookie.Create(true)
            use disableFormatter = new DisableCodeFormatter()

            for highlightingInfo in highlightingInfos do
                let warning = highlightingInfo.Highlighting.As<UnusedValueWarning>()
                if isNotNull warning && ReplaceWithWildPat.isAvailable warning.Pat then
                    ReplaceWithWildPat.replaceWithWildPat warning.Pat

            null
