module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.RedundantParenTypeUsageAnalyzer

open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

let applicable (typeUsage: ITypeUsage) =
    not (typeUsage :? IUnsupportedTypeUsage) // todo: remove when all FSC types usages are properly mapped

let rec getLongestReturnFromArg (typeUsage: ITypeUsage) =
    let typeUsage = typeUsage.IgnoreParentParens()
    match FunctionTypeUsageNavigator.GetByArgumentTypeUsage(typeUsage) with
    | null -> if typeUsage :? IFunctionTypeUsage then typeUsage else typeUsage
    | typeUsage -> getLongestReturnFromArg typeUsage

let rec getLongestReturnFromReturn (typeUsage: ITypeUsage) =
    match FunctionTypeUsageNavigator.GetByReturnTypeUsage(typeUsage.IgnoreParentParens()) with
    | null -> typeUsage
    | typeUsage -> getLongestReturnFromReturn typeUsage

let rec ignoreParentCompoundTypes (typeUsage: ITypeUsage) =
    let parent = typeUsage.Parent
    match parent with
    | :? ITupleTypeUsage
    | :? IFunctionTypeUsage -> ignoreParentCompoundTypes (parent :?> _)
    | _ -> typeUsage

let rec getFirstArg (functionTypeUsage: IFunctionTypeUsage) =
    match functionTypeUsage.ArgumentTypeUsage.IgnoreInnerParens() with
    | :? IFunctionTypeUsage as argFunctionTypeUsage -> getFirstArg argFunctionTypeUsage
    | argTypeUsage -> argTypeUsage

let rec requiresParensInAbbreviation (typeUsage: ITypeUsage) =
    match typeUsage.IgnoreInnerParens() with
    | :? ITupleTypeUsage as tupleTypeUsage -> tupleTypeUsage.IsStruct
    | :? IFunctionTypeUsage as functionTypeUsage ->
        let argTypeUsage = getFirstArg functionTypeUsage
        requiresParensInAbbreviation argTypeUsage
    | _ -> false

let needsParens (context: ITypeUsage) (typeUsage: ITypeUsage): bool =
    let parentTypeUsage = context.Parent.As<ITypeUsage>()
    if isNotNull parentTypeUsage && not (applicable parentTypeUsage) then true else
    if context.Parent :? ITraitCallExpr then true else

    match typeUsage with
    | :? ITupleTypeUsage ->
        let functionTypeUsage = FunctionTypeUsageNavigator.GetByReturnTypeUsage(context)
        let argTypeUsage = getLongestReturnFromArg context

        let paramSig =
            let paramSig = ParameterSignatureTypeUsageNavigator.GetByTypeUsage(context)
            if isNotNull paramSig then paramSig else

            let paramSig = ParameterSignatureTypeUsageNavigator.GetByTypeUsage(functionTypeUsage)
            if isNotNull paramSig then paramSig else

            let paramSig = ParameterSignatureTypeUsageNavigator.GetByTypeUsage(argTypeUsage)
            if isNotNull paramSig then paramSig else

            null

        if isNotNull paramSig && (isNull (FSharpTypeOwnerDeclarationNavigator.GetByTypeUsage(paramSig))) then true else

        let isInAbbreviation = isNotNull (TypeAbbreviationRepresentationNavigator.GetByAbbreviatedType(argTypeUsage))
        if isInAbbreviation && requiresParensInAbbreviation argTypeUsage then true else

        isNotNull (TupleTypeUsageNavigator.GetByItem(context)) ||
        isNotNull (ArrayTypeUsageNavigator.GetByTypeUsage(context)) ||
        isNotNull (PostfixAppTypeArgumentListNavigator.GetByTypeUsage(context)) ||
        isNotNull (IsInstPatNavigator.GetByTypeUsage(context)) ||
        isNotNull (CaseFieldDeclarationNavigator.GetByTypeUsage(context)) ||
        isNotNull (IsInstPatNavigator.GetByTypeUsage(ignoreParentCompoundTypes context))

    | :? IFunctionTypeUsage ->
        isNotNull (TupleTypeUsageNavigator.GetByItem(context)) ||
        isNotNull (FunctionTypeUsageNavigator.GetByArgumentTypeUsage(context)) ||
        isNotNull (ArrayTypeUsageNavigator.GetByTypeUsage(context)) ||
        isNotNull (PostfixAppTypeArgumentListNavigator.GetByTypeUsage(context)) ||
        isNotNull (IsInstPatNavigator.GetByTypeUsage(context)) ||
        isNotNull (ParameterSignatureTypeUsageNavigator.GetByTypeUsage(context)) ||
        isNotNull (CaseFieldDeclarationNavigator.GetByTypeUsage(context)) ||
        isNotNull (IsInstPatNavigator.GetByTypeUsage(ignoreParentCompoundTypes context)) ||

        let longestReturn = getLongestReturnFromReturn context
        isNotNull (ValFieldDeclarationNavigator.GetByTypeUsage(longestReturn)) ||

        let isInAbbreviation = isNotNull (TypeAbbreviationRepresentationNavigator.GetByAbbreviatedType(context))
        isInAbbreviation && requiresParensInAbbreviation typeUsage

    | :? IArrayTypeUsage ->
        isNotNull (IsInstPatNavigator.GetByTypeUsage(ignoreParentCompoundTypes context))

    | :? INamedTypeUsage as namedTypeUsage ->
        isNotNull (IsInstPatNavigator.GetByTypeUsage(context)) &&

        let referenceName = namedTypeUsage.ReferenceName
        isNotNull referenceName && referenceName.TypeArgumentList :? IPostfixAppTypeArgumentList

    | :? IWithNullTypeUsage ->
        isNotNull (ArrayTypeUsageNavigator.GetByTypeUsage(context))

    | _ -> false
