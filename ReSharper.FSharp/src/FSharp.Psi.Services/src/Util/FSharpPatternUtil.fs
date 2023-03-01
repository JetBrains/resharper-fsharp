module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpPatternUtil

open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell

let getReferenceName (pattern: IFSharpPattern) =
    // todo: unify interface
    match pattern with
    | :? IReferencePat as refPat -> refPat.ReferenceName
    | :? IParametersOwnerPat as p -> p.ReferenceName
    | _ -> null

let toParameterOwnerPat (pat: IFSharpPattern) opName =
    use writeCookie = WriteLockCookie.Create(pat.IsPhysical())
    use transactionCookie = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(pat.GetPsiServices(), opName)

    match pat with
    | :? IReferencePat as refPat ->
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let referenceName = refPat.ReferenceName.NotNull()
        let factory = pat.CreateElementFactory()
        let newPattern = factory.CreatePattern("(__ _)", false) :?> IParenPat
        let newPat = ModificationUtil.ReplaceChild(refPat, newPattern.Pattern) :?> IParametersOwnerPat
        ModificationUtil.ReplaceChild(newPat.ReferenceName, referenceName) |> ignore
        newPat
    | _ -> failwith $"Unexpected pattern: {pat}"

let bindFcsSymbolToReference (context: ITreeNode) (referenceName: IReferenceName) (fcsSymbol: FSharpSymbol) opName =
    let declaredElement = fcsSymbol.GetDeclaredElement(context.GetPsiModule()).As<IClrDeclaredElement>()
    if isNull referenceName || referenceName.IsQualified || isNull declaredElement then () else

    let reference = referenceName.Reference
    FSharpReferenceBindingUtil.SetRequiredQualifiers(reference, declaredElement)

    if not (FSharpResolveUtil.resolvesToQualified declaredElement reference true opName) then
        // todo: use declared element directly
        let typeElement = declaredElement.GetContainingType()
        addOpens reference typeElement |> ignore

// todo: replace Fcs symbols with R# elements when possible
let bindFcsSymbol (pattern: IFSharpPattern) (fcsSymbol: FSharpSymbol) opName =
    // todo: move to reference binding
    let bind name =
        let factory = pattern.CreateElementFactory()

        let name = PrettyNaming.NormalizeIdentifierBackticks name
        let newPattern = factory.CreatePattern(name, false)
        let pat = ModificationUtil.ReplaceChild(pattern, newPattern)

        let referenceName = getReferenceName pat

        let oldQualifierWithDot =
            let referenceName = getReferenceName pattern
            if isNotNull referenceName then TreeRange(referenceName.Qualifier, referenceName.Delimiter) else null

        if isNotNull oldQualifierWithDot then
            ModificationUtil.AddChildRangeAfter(referenceName, null, oldQualifierWithDot) |> ignore

        bindFcsSymbolToReference pat referenceName fcsSymbol opName

        pat
    
    match fcsSymbol with
    | :? FSharpUnionCase as unionCase -> bind unionCase.Name
    | :? FSharpField as field when FSharpSymbolUtil.isEnumMember field -> bind field.Name
    | _ -> failwith $"Unexpected symbol: {fcsSymbol}"

let rec ignoreParentAsPatsFromRight (pat: IFSharpPattern) =
    match AsPatNavigator.GetByRightPattern(pat.IgnoreParentParens()) with
    | null -> pat
    | pat -> ignoreParentAsPatsFromRight pat

let rec ignoreInnerAsPatsToRight (pat: IFSharpPattern) =
    match pat with
    | :? IAsPat as asPat -> ignoreInnerAsPatsToRight asPat.RightPattern
    | _ -> pat

module ParentTraversal =
    [<RequireQualifiedAccess>]
    type PatternParentTraverseStep =
        | Tuple of item: int * tuplePat: ITuplePat
        | Or of orPat: IOrPat
        | And of andPat: IAndsPat
        | As of asPat: IAsPat

    let makeTuplePatPath pat =
        let rec tryMakePatPath path (IgnoreParenPat fsPattern: IFSharpPattern) =
            let asPat = AsPatNavigator.GetByLeftPattern(fsPattern)
            if isNotNull asPat then
                tryMakePatPath (PatternParentTraverseStep.As(asPat) :: path) asPat else

            let tuplePat = TuplePatNavigator.GetByPattern(fsPattern)
            if isNotNull tuplePat then
                let item = tuplePat.Patterns.IndexOf(fsPattern)
                Assertion.Assert(item <> -1, "item <> -1")
                tryMakePatPath (PatternParentTraverseStep.Tuple(item, tuplePat) :: path) tuplePat else

            let orPat = OrPatNavigator.GetByPattern(fsPattern)
            if isNotNull orPat then
                tryMakePatPath (PatternParentTraverseStep.Or(orPat) :: path) orPat else

            let andsPat = AndsPatNavigator.GetByPattern(fsPattern)
            if isNotNull andsPat then
                tryMakePatPath (PatternParentTraverseStep.And(andsPat) :: path) andsPat else

            fsPattern, path

        tryMakePatPath [] pat

    let rec tryTraverseExprPath (path: PatternParentTraverseStep list) (IgnoreInnerParenExpr expr) =
        match path with
        | [] -> expr
        | step :: rest ->

        match expr, step with
        | _, (PatternParentTraverseStep.Or _ | PatternParentTraverseStep.And _ | PatternParentTraverseStep.As _) ->
            tryTraverseExprPath rest expr

        | :? ITupleExpr as tupleExpr, PatternParentTraverseStep.Tuple(n, _) ->
            let tupleItems = tupleExpr.Expressions
            if tupleItems.Count <= n then null else
            tryTraverseExprPath rest tupleItems[n]

        | _ -> null

    let tryFindSourceExpr (pat: IFSharpPattern) =
        let pat, path = makeTuplePatPath pat
        let matchClause = MatchClauseNavigator.GetByPattern(pat)
        let matchExpr = MatchExprNavigator.GetByClause(matchClause)
        if isNull matchExpr then null else

        tryTraverseExprPath path matchExpr.Expression
