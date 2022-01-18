module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.RedundantParenTypeUsageAnalyzer

open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

let applicable (typeUsage: ITypeUsage) =
    not (typeUsage :? IUnsupportedTypeUsage) // todo: remove when all FSC types usages are properly mapped

let rec getLongestReturnFromArg (typeUsage: ITypeUsage) =
    match FunctionTypeUsageNavigator.GetByArgumentTypeUsage(typeUsage.IgnoreParentParens()) with
    | null -> if typeUsage :? IFunctionTypeUsage then typeUsage else null
    | typeUsage -> getLongestReturnFromArg typeUsage

let rec getLongestReturnFromReturn (typeUsage: ITypeUsage) =
    match FunctionTypeUsageNavigator.GetByReturnTypeUsage(typeUsage.IgnoreParentParens()) with
    | null -> typeUsage
    | typeUsage -> getLongestReturnFromReturn typeUsage

let rec ignoreParentCompoundTypes (typeUsage: ITypeUsage) =
    let parent = typeUsage.Parent
    match parent with
    | :? ITupleTypeUsage
    | :? IFunctionTypeUsage -> ignoreParentCompoundTypes (parent :?> ITypeUsage)
    | _ -> typeUsage

let needsParens (context: ITypeUsage) (typeUsage: ITypeUsage): bool =
    let parentTypeUsage = context.Parent.As<ITypeUsage>()
    if isNotNull parentTypeUsage && not (applicable parentTypeUsage) then true else

    match typeUsage with
    | :? ITupleTypeUsage as tupleTypeUsage ->
        // todo: rewrite when top-level-types are supported
        let functionTypeUsage = FunctionTypeUsageNavigator.GetByReturnTypeUsage(context)
        if isNotNull (ParameterSignatureTypeUsageNavigator.GetByTypeUsage(context)) then true else
        if isNotNull (ParameterSignatureTypeUsageNavigator.GetByTypeUsage(functionTypeUsage)) then true else
        if isNotNull (ParameterSignatureTypeUsageNavigator.GetByTypeUsage(getLongestReturnFromArg context)) then true else

        let isStruct = isNotNull tupleTypeUsage.StructKeyword
        if isStruct && isNotNull (TypeAbbreviationRepresentationNavigator.GetByAbbreviatedType(context)) then true else

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
        isNotNull (ReturnTypeInfoNavigator.GetByReturnType(longestReturn)) ||
        isNotNull (ValFieldDeclarationNavigator.GetByTypeUsage(longestReturn))

    | :? IArrayTypeUsage ->
        isNotNull (IsInstPatNavigator.GetByTypeUsage(ignoreParentCompoundTypes context))

    | _ -> false
