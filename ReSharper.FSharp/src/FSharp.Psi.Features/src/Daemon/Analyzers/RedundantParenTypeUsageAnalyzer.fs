namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

module RedundantParenTypeUsageAnalyzer =
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
        | :? ITupleTypeUsage ->
            // todo: rewrite when top-level-types are supported
            let functionTypeUsage = FunctionTypeUsageNavigator.GetByReturnTypeUsage(context)
            if isNotNull (ParameterSignatureTypeUsageNavigator.GetByType(context)) then true else
            if isNotNull (ParameterSignatureTypeUsageNavigator.GetByType(functionTypeUsage)) then true else
            if isNotNull (ParameterSignatureTypeUsageNavigator.GetByType(getLongestReturnFromArg context)) then true else

            isNotNull (TupleTypeUsageNavigator.GetByItem(context)) ||
            isNotNull (ArrayTypeUsageNavigator.GetByType(context)) ||
            isNotNull (PostfixAppTypeArgumentListNavigator.GetByType(context)) ||
            isNotNull (IsInstPatNavigator.GetByType(context)) ||
            isNotNull (CaseFieldDeclarationNavigator.GetByType(context)) ||
            isNotNull (IsInstPatNavigator.GetByType(ignoreParentCompoundTypes context))

        | :? IFunctionTypeUsage ->
            isNotNull (TupleTypeUsageNavigator.GetByItem(context)) ||
            isNotNull (FunctionTypeUsageNavigator.GetByArgumentTypeUsage(context)) ||
            isNotNull (ArrayTypeUsageNavigator.GetByType(context)) ||
            isNotNull (PostfixAppTypeArgumentListNavigator.GetByType(context)) ||
            isNotNull (IsInstPatNavigator.GetByType(context)) ||
            isNotNull (ParameterSignatureTypeUsageNavigator.GetByType(context)) ||
            isNotNull (CaseFieldDeclarationNavigator.GetByType(context)) ||
            isNotNull (IsInstPatNavigator.GetByType(ignoreParentCompoundTypes context)) ||

            let longestReturn = getLongestReturnFromReturn context
            isNotNull (ReturnTypeInfoNavigator.GetByReturnType(longestReturn)) ||
            isNotNull (ValFieldDeclarationNavigator.GetByType(longestReturn))

        | :? IArrayTypeUsage ->
            isNotNull (IsInstPatNavigator.GetByType(ignoreParentCompoundTypes context))

        | _ -> false

open RedundantParenTypeUsageAnalyzer

[<ElementProblemAnalyzer(typeof<IParenTypeUsage>, HighlightingTypes = [| typeof<RedundantParenTypeUsageWarning> |])>]
type RedundantParenTypeUsageAnalyzer() =
    inherit ElementProblemAnalyzer<IParenTypeUsage>()

    override this.Run(parenTypeUsage, _, consumer) =
        if isNull parenTypeUsage.LeftParen || isNull parenTypeUsage.RightParen then () else

        let typeUsage = parenTypeUsage.InnerTypeUsage
        let context = typeUsage.IgnoreParentParens()  

        if typeUsage :? IParenTypeUsage || applicable typeUsage && not (needsParens context typeUsage) then
            consumer.AddHighlighting(RedundantParenTypeUsageWarning(parenTypeUsage))

    interface IFSharpRedundantParenAnalyzer
