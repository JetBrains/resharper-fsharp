namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Scopes
open JetBrains.ReSharper.Feature.Services.QuickFixes.Scoped
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

// todo: combine with RemoveUnusedNamedAsPat:
//   match () with
//   | _ as a & _
//   | a & _ -> ()

module ReplaceWithWildPat =
    let replaceWithWildPat (pat: IFSharpPattern) =
        let factory = pat.GetFSharpLanguageService().CreateElementFactory(pat)
        for pat in pat.GetPartialDeclarations() do
            replace pat (factory.CreateWildPat())

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

        let pat = FSharpPatternUtil.ignoreParentAsPatsFromRight pat
        let binding = BindingNavigator.GetByHeadPattern(pat.IgnoreParentParens())
        if isNotNull binding && binding.HasParameters then false else

        match pat with
        | :? IParametersOwnerPat
        | :? IAsPat -> false // todo: allow for 'as' patterns, check if inner patterns are used
        | _ ->

        let node = getPatOwner pat
        if isNull node then false else

        match node.Parent with
        | :? IBinding | :? IMatchClause | :? ILambdaParametersList | :? IForEachExpr -> true

        | :? IParametersPatternDeclaration as parent ->
            let parent = parent.Parent
            parent :? IMemberDeclaration || parent :? ISecondaryConstructorDeclaration || parent :? IBinding

        | _ -> false


type ReplaceWithWildPatScopedFix(pat: IFSharpPattern, highlightingType) =
    inherit FSharpScopedQuickFixBase(pat)

    new (warning: RedundantUnionCaseFieldPatternsWarning) =
        ReplaceWithWildPatScopedFix(warning.ParenPat, warning.GetType())

    override x.Text = "Replace with '_'"

    override this.GetScopedFixingStrategy(_, _) =
        SameQuickFixSameHighlightingTypeStrategy(highlightingType, this) :> _

    override x.IsAvailable _ =
        ReplaceWithWildPat.isAvailable pat

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(pat.IsPhysical())
        ReplaceWithWildPat.replaceWithWildPat pat


type ReplaceWithWildPatFix(pat: IFSharpPattern, isFromUnusedValue) =
    inherit FSharpScopedNonIncrementalQuickFixBase(pat)

    let patOwner = ReplaceWithWildPat.getPatOwner pat

    new (warning: UnusedValueWarning) =
        ReplaceWithWildPatFix(warning.Pat, true)

    new (error: VarBoundTwiceError) =
        ReplaceWithWildPatFix(error.Pat, false)

    override x.Text = "Replace with '_'"
    override x.ScopedText = "Replace unused values with '_'"

    override x.FileCollectorInfo =
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
                match BindingNavigator.GetByParametersDeclaration(p) with
                | null -> pat :> _, "parameter list"
                | binding -> binding :> _, "binding patterns"

            | :? ILambdaParametersList as parametersList -> parametersList :> _, "parameter list"
            | :? IBinding -> pat :> _, "binding patterns"
            | :? IForEachExpr -> pat :> _, "'for' pattern"
            | _ -> invalidArg "patOwner.Parent" "unexpected type"

        FileCollectorInfo.WithLocalAndAdditionalScopes(scopeNode, LocalScope(scopeNode, $"in {scopeText}"))

    override this.IsReanalysisRequired = false
    override this.ReanalysisDependencyRoot = null

    override x.IsAvailable _ = ReplaceWithWildPat.isAvailable pat

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        ReplaceWithWildPat.replaceWithWildPat pat
