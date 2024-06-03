module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpPatternUtil

open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
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

let rec ignoreParentAsPatsFromRight (pat: IFSharpPattern) =
    match AsPatNavigator.GetByRightPattern(pat.IgnoreParentParens()) with
    | null -> pat
    | pat -> ignoreParentAsPatsFromRight pat

let rec ignoreInnerAsPatsToRight (pat: IFSharpPattern) =
    match pat with
    | :? IAsPat as asPat -> ignoreInnerAsPatsToRight asPat.RightPattern
    | _ -> pat

// todo: try to unify with match test patterns?

module ParentTraversal =
    [<RequireQualifiedAccess>]
    type PatternParentTraverseStep =
        | Tuple of index: int * tuplePat: ITuplePat
        | Or of index: int * orPat: IOrPat
        | And of index: int * andPat: IAndsPat
        | As of index: int * asPat: IAsPat
        | IsInst of isInstPat: IIsInstPat
        | ParameterOwner of paramOwnerPat: IParametersOwnerPat
        | List of index: int * pat: IArrayOrListPat
        | ListCons of index: int * pat: IListConsPat
        | Field of fieldName: string * pat: IFieldPat
        | Record of pat: IRecordPat
        | Error

    let makePatPath pat =
        let rec tryMakePatPath path (IgnoreParenPat pat: IFSharpPattern) =
            let parent = pat.Parent.As<IFSharpPattern>()

            let step =
                match parent with
                | :? IAsPat as asPat ->
                    let index = if asPat.LeftPattern == pat then 0 else 1 
                    PatternParentTraverseStep.As(index, asPat)

                | :? ITuplePat as tuplePat ->
                    let index = tuplePat.Patterns.IndexOf(pat)
                    PatternParentTraverseStep.Tuple(index, tuplePat)

                | :? IOrPat as orPat ->
                    let index = if orPat.Pattern1 == pat then 0 else 1
                    PatternParentTraverseStep.Or(index, orPat)

                | :? IAndsPat as andsPat ->
                    let index = andsPat.Patterns.IndexOf(pat)
                    PatternParentTraverseStep.And(index, andsPat)

                | :? IParametersOwnerPat as parametersOwnerPat ->
                    PatternParentTraverseStep.ParameterOwner(parametersOwnerPat)

                | :? IArrayOrListPat as listPat ->
                    let index = listPat.Patterns.IndexOf(pat)
                    PatternParentTraverseStep.List(index, listPat)

                | :? IListConsPat as listConsPat ->
                    let index = if listConsPat.HeadPattern == pat then 0 else 1
                    PatternParentTraverseStep.ListCons(index, listConsPat)

                | :? IFieldPat as fieldPat ->
                    PatternParentTraverseStep.Field(fieldPat.ShortName, fieldPat)

                | :? IRecordPat as recordPat ->
                    PatternParentTraverseStep.Record(recordPat)

                | _ -> PatternParentTraverseStep.Error

            match step with
            | PatternParentTraverseStep.Error -> pat, path
            | _ -> tryMakePatPath (step :: path) parent

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
        let pat, path = makePatPath pat
        let matchClause = MatchClauseNavigator.GetByPattern(pat)
        let matchExpr = MatchExprNavigator.GetByClause(matchClause)
        if isNull matchExpr then null else

        tryTraverseExprPath path matchExpr.Expression
