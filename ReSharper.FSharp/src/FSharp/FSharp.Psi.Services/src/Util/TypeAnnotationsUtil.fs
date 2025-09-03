module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationsUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpPatternUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

let rec private collectPatterns (acc: ITreeNode list) (root: IFSharpPattern) =
    match root with
    | null
    | :? IConstPat
    | :? IWildPat
    | :? ITypedPat -> acc

    | :? IParenPat as parenPat -> collectPatterns acc parenPat.Pattern
    | :? IAsPat as asPat -> collectPatterns acc asPat.LeftPattern

    | :? IOptionalValPat
    | :? IReferencePat as pat -> pat :: acc

    | :? IRecordPat as recordPat ->
        recordPat.FieldPatternsEnumerable
        |> Seq.fold collectPatterns acc

    | :? IFieldPat as fieldPat -> collectPatterns acc fieldPat.Pattern
    | :? IAttribPat as attribPat -> collectPatterns acc attribPat.Pattern

    | :? ITuplePat as tuplePat ->
        tuplePat.PatternsEnumerable
        |> Seq.fold collectPatterns acc

    | :? IParametersOwnerPat as parametersOwnerPat ->
        let acc = (parametersOwnerPat : ITreeNode) :: acc
        parametersOwnerPat.ParametersEnumerable
        |> Seq.fold collectPatterns acc

    | :? INamedUnionCaseFieldsPat as unionCaseFieldsPat ->
        unionCaseFieldsPat.FieldPatternsEnumerable
        |> Seq.fold collectPatterns acc

    | :? IAndsPat as andPat ->
        andPat.PatternsEnumerable
        |> Seq.fold collectPatterns acc

    | :? IOrPat as orPat ->
        collectPatterns acc orPat.Pattern1

    | :? IListConsPat as pat ->
        let tailPat = getLastTailPattern pat
        if isNull tailPat || tailPat :? ITypedPat then acc else tailPat :: acc

    | :? IArrayOrListPat as pat ->
        pat :: acc

    | _ -> acc

let private collectPatternsRequiringAnnotations acc (parametersOwner: IParameterOwnerMemberDeclaration) =
    parametersOwner.ParameterPatterns
    |> Seq.fold collectPatterns acc

let collectTypeHintAnchorsForBinding (binding: IBinding) =
    let acc: ITreeNode list = []

    if isNull binding || isNull binding.EqualsToken then acc else

    let acc =
        if isNotNull binding.ReturnTypeInfo then acc
        elif binding.HasParameters then [binding :> ITreeNode]
        else collectPatterns acc binding.HeadPattern

    collectPatternsRequiringAnnotations acc binding

let collectTypeHintAnchorsForLambda (lambda: ILambdaExpr) =
    let acc: ITreeNode list = []

    if isNull lambda || isNull lambda.RArrow then acc else

    lambda.PatternsEnumerable
    |> Seq.fold collectPatterns acc

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
    else collectPatterns [] forEachExpr.Pattern

let collectTypeHintAnchorsForMatchClause (matchClause: IMatchClause) =
    if isNull matchClause.RArrow then []
    else collectPatterns [] matchClause.Pattern
