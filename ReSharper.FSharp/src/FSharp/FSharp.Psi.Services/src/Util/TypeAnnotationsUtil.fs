module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationsUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

let private isRequiresAnnotation (pattern: IFSharpPattern) =
    match pattern with
    | :? IReferencePat -> true
    | :? IArrayOrListPat as pat when pat.PatternsEnumerable.IsEmpty() -> true
    | _ -> false

let rec private tryVisitCompositePattern acc (pattern: IFSharpPattern) =
    if isRequiresAnnotation pattern then acc
    else visitPattern acc pattern

and private collectHintsForCollectionPattern innerPatterns acc defaultPatternToAnnotate=
    let acc =
        match List.filter isRequiresAnnotation innerPatterns with
        | [] -> acc
        | [pattern] -> visitPattern acc pattern
        | _ ->

        match defaultPatternToAnnotate with
        | ValueNone -> acc
        | ValueSome pat -> pat :: acc

    innerPatterns
    |> Seq.fold tryVisitCompositePattern acc

and private visitPattern (acc: ITreeNode list) (pattern: IFSharpPattern) =
    match pattern with
    | null
    | :? IUnitPat
    | :? IWildPat
    | :? ITypedPat -> acc

    | :? IParenPat as parenPat -> visitPattern acc parenPat.Pattern
    | :? IAsPat as asPat -> visitPattern acc asPat.LeftPattern

    | :? IReferencePat as pat -> pat :: acc

    | :? IRecordPat as recordPat ->
        recordPat.FieldPatternsEnumerable
        |> Seq.fold visitPattern acc

    | :? IFieldPat as fieldPat -> visitPattern acc fieldPat.Pattern
    | :? IAttribPat as attribPat -> visitPattern acc attribPat.Pattern

    | :? ITuplePat as tuplePat ->
        tuplePat.PatternsEnumerable
        |> Seq.fold visitPattern acc

    | :? IParametersOwnerPat as parametersOwnerPat ->
        let acc = (parametersOwnerPat : ITreeNode) :: acc
        parametersOwnerPat.ParametersEnumerable
        |> Seq.fold visitPattern acc

    | :? INamedUnionCaseFieldsPat as unionCaseFieldsPat ->
        unionCaseFieldsPat.FieldPatternsEnumerable
        |> Seq.fold visitPattern acc

    | :? IAndsPat as andPat ->
        andPat.PatternsEnumerable
        |> Seq.fold visitPattern acc

    | :? IOrPat as orPat ->
        visitPattern acc orPat.Pattern1

    | :? IListConsPat as listConsPat ->
        let rec flat (pat: IFSharpPattern) acc =
            match pat.IgnoreInnerParens() with
            | :? IListConsPat as pat -> flat pat.TailPattern (pat.HeadPattern :: acc)
            | _ -> pat :: acc

        let patterns = flat listConsPat []

        let defaultPatternToAnnotate: ITreeNode voption =
            match patterns with
            | :? IWildPat as tailPat :: _ -> ValueSome(tailPat)
            | tailPat :: _ when isRequiresAnnotation tailPat -> ValueSome(tailPat)
            | _ -> ValueNone

        collectHintsForCollectionPattern patterns acc defaultPatternToAnnotate

    | :? IArrayOrListPat as arrayOrListPat ->
        let patterns = arrayOrListPat.PatternsEnumerable
        if patterns.IsEmpty() then arrayOrListPat :: acc else

        let patterns = List.ofSeq patterns
        let defaultPatternToAnnotate = ValueSome(arrayOrListPat : ITreeNode)
        collectHintsForCollectionPattern patterns acc defaultPatternToAnnotate

    | _ -> acc

let private collectPatternsRequiringAnnotations acc (parametersOwner: IParameterOwnerMemberDeclaration) =
    parametersOwner.ParameterPatterns
    |> Seq.fold visitPattern acc

let collectTypeHintAnchorsForBinding (binding: IBinding) =
    let acc: ITreeNode list = []

    if isNull binding || isNull binding.EqualsToken then acc else

    let acc =
        if isNotNull binding.ReturnTypeInfo then acc
        elif binding.HasParameters then [binding :> ITreeNode]
        else  visitPattern acc binding.HeadPattern

    collectPatternsRequiringAnnotations acc binding

let collectTypeHintAnchorsForLambda (lambda: ILambdaExpr) =
    let acc: ITreeNode list = []

    if isNull lambda || isNull lambda.RArrow then acc else

    lambda.PatternsEnumerable
    |> Seq.fold visitPattern acc

let collectTypeHintsAnchorsForMember (m: IMemberDeclaration) =
    let accessorDeclarations = m.AccessorDeclarations
    if isNull m || isNull m.EqualsToken && accessorDeclarations.IsEmpty then [] else

    let acc = if isNull m.ReturnTypeInfo then [m :> ITreeNode] else []
    let acc = collectPatternsRequiringAnnotations acc m

    accessorDeclarations
    |> Seq.fold collectPatternsRequiringAnnotations acc

let collectTypeHintAnchorsForConstructor (ctor: IConstructorDeclaration) =
    if isNull ctor.EqualsToken then []
    else collectPatternsRequiringAnnotations [] ctor

let collectTypeHintAnchorsForEachExpr (forEachExpr: IForEachExpr) =
    if isNull forEachExpr.InExpression then []
    else visitPattern [] forEachExpr.Pattern

let collectTypeHintAnchorsForMatchClause (matchClause: IMatchClause) =
    if isNull matchClause.RArrow then []
    else visitPattern [] matchClause.Pattern
