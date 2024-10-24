module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationsUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

let rec visitConsTailPat (acc: ITreeNode list) (pattern: IFSharpPattern) =
    match pattern.IgnoreInnerParens() with
    | :? IReferencePat -> acc
    | :? IListConsPat as listConsPat ->
        let acc = visitConsTailPat acc listConsPat.HeadPattern
        visitConsTailPat acc listConsPat.TailPattern
    | _ -> visitPattern acc pattern

and visitArrayOrListTailPat (acc: ITreeNode list) (pattern: IFSharpPattern) =
    match pattern.IgnoreInnerParens() with
    | :? IReferencePat -> acc
    | _ -> visitPattern acc pattern

and visitPattern (acc: ITreeNode list) (pattern: IFSharpPattern) =
    match pattern with
    | null -> acc
    | :? IListConsPat as listConsPat ->
        let acc = visitPattern acc listConsPat.HeadPattern
        visitConsTailPat acc listConsPat.TailPattern
    | :? IArrayOrListPat as seq ->
        let nestedPatterns = seq.NestedPatterns
        if Seq.isEmpty nestedPatterns then acc else

        match nestedPatterns |> Seq.tryHead with
        | Some head ->
            let acc = visitPattern acc head
            nestedPatterns
            |> Seq.tail
            |> Seq.fold visitArrayOrListTailPat acc
        | None -> acc

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
