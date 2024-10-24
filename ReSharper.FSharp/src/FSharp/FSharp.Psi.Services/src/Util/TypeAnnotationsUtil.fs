module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationsUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

let rec private visitPattern (acc: ITreeNode list) (pattern: IFSharpPattern) =
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
