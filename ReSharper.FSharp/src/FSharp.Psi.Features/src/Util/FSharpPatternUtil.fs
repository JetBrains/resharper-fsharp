module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpPatternUtil

open FSharp.Compiler.Symbols
open FSharp.Compiler.Tokenization
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Util

// todo: replace Fcs symbols with R# elements when possible
let bindFcsSymbol (pattern: IFSharpPattern) (fcsSymbol: FSharpSymbol) opName =
    let getReferenceName (pattern: IFSharpPattern) =
        // todo: unify interface
        match pattern with
        | :? IReferencePat as refPat -> refPat.ReferenceName
        | :? IParametersOwnerPat as p -> p.ReferenceName
        | _ -> null

    let bind patternText =
        let factory = pattern.CreateElementFactory()

        let newPattern = factory.CreatePattern(patternText, false) :?> IParenPat
        let pat = ModificationUtil.ReplaceChild(pattern, newPattern.Pattern) // todo: move to reference binding

        let referenceName = getReferenceName pat

        let oldQualifierWithDot =
            let referenceName = getReferenceName pattern
            if isNotNull referenceName then TreeRange(referenceName.Qualifier, referenceName.Delimiter) else null

        if isNotNull oldQualifierWithDot then
            ModificationUtil.AddChildRangeAfter(referenceName, null, oldQualifierWithDot) |> ignore

        let declaredElement = fcsSymbol.GetDeclaredElement(pat.GetPsiModule()).As<IClrDeclaredElement>()
        if isNull referenceName || referenceName.IsQualified || isNull declaredElement then pat else

        let reference = referenceName.Reference
        FSharpReferenceBindingUtil.SetRequiredQualifiers(reference, declaredElement)

        if not (FSharpResolveUtil.resolvesToQualified declaredElement reference true opName) then
            // todo: use declared element directly
            let typeElement = declaredElement.GetContainingType()
            addOpens reference typeElement |> ignore

        pat
    
    match fcsSymbol with
    | :? FSharpUnionCase as unionCase ->
        let name = FSharpKeywords.AddBackticksToIdentifierIfNeeded unionCase.Name
        let text = if unionCase.HasFields then $"({name} _)" else $"({name})" // todo: remove parens, escape in factory
        bind text

    | :? FSharpField as field when FSharpSymbolUtil.isEnumMember field ->
        let name = FSharpKeywords.AddBackticksToIdentifierIfNeeded field.Name
        let text = $"({name})"
        bind text

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

    let makeTuplePatPath pat =
        let rec tryMakePatPath path (IgnoreParenPat fsPattern: IFSharpPattern) =
            match fsPattern.Parent with
            | :? ITuplePat as tuplePat ->
                let item = tuplePat.Patterns.IndexOf(fsPattern)
                Assertion.Assert(item <> -1, "item <> -1")
                tryMakePatPath (PatternParentTraverseStep.Tuple(item, tuplePat) :: path) tuplePat

            | :? IOrPat as orPat ->
                tryMakePatPath (PatternParentTraverseStep.Or(orPat) :: path) orPat

            | :? IAndsPat as andsPat ->
                tryMakePatPath (PatternParentTraverseStep.And(andsPat) :: path) andsPat

            | _ -> fsPattern, path

        tryMakePatPath [] pat

    let rec tryTraverseExprPath (path: PatternParentTraverseStep list) (IgnoreInnerParenExpr expr) =
        match path with
        | [] -> expr
        | step :: rest ->

        match expr, step with
        | _, (PatternParentTraverseStep.Or _ | PatternParentTraverseStep.And _) ->
            tryTraverseExprPath rest expr

        | :? ITupleExpr as tupleExpr, PatternParentTraverseStep.Tuple(n, _) ->
            let tupleItems = tupleExpr.Expressions
            if tupleItems.Count <= n then null else
            tryTraverseExprPath rest tupleItems.[n]

        | _ -> null
