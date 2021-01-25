namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer(typeof<IParenTypeUsage>, HighlightingTypes = [| typeof<RedundantParenTypeUsageWarning> |])>]
type RedundantParenTypeUsageAnalyzer() =
    inherit ElementProblemAnalyzer<IParenTypeUsage>()

    let applicable (typeUsage: ITypeUsage) =
        not (typeUsage :? IUnsupportedTypeUsage) // todo: remove when all FSC types usages are properly mapped

    let rec getLongestReturn (typeUsage: ITypeUsage) =
        match FunctionTypeUsageNavigator.GetByArgumentTypeUsage(typeUsage.IgnoreParentParens()) with
        | null -> if typeUsage :? IFunctionTypeUsage then typeUsage else null
        | typeUsage -> getLongestReturn typeUsage

    let needsParens (context: ITypeUsage) (typeUsage: ITypeUsage): bool =
        let parentTypeUsage = context.Parent.As<ITypeUsage>()
        if isNotNull parentTypeUsage && not (applicable parentTypeUsage) then true else

        match typeUsage with
        | :? ITupleTypeUsage ->
            // todo: rewrite when top-level-types are supported
            let functionTypeUsage = FunctionTypeUsageNavigator.GetByReturnTypeUsage(context)
            if isNotNull (MemberSignatureLikeDeclarationNavigator.GetByType(context)) then true else
            if isNotNull (MemberSignatureLikeDeclarationNavigator.GetByType(functionTypeUsage)) then true else
            if isNotNull (MemberSignatureLikeDeclarationNavigator.GetByType(getLongestReturn context)) then true else

            isNotNull (TupleTypeUsageNavigator.GetByItem(context)) ||
            isNotNull (ArrayTypeUsageNavigator.GetByType(context)) ||
            isNotNull (PostfixAppTypeArgumentListNavigator.GetByType(context)) ||
            isNotNull (CaseFieldDeclarationNavigator.GetByType(context))

        | :? IFunctionTypeUsage ->
            isNotNull (TupleTypeUsageNavigator.GetByItem(context)) ||
            isNotNull (FunctionTypeUsageNavigator.GetByArgumentTypeUsage(context)) ||
            isNotNull (ArrayTypeUsageNavigator.GetByType(context)) ||
            isNotNull (PostfixAppTypeArgumentListNavigator.GetByType(context)) ||
            isNotNull (IsInstPatNavigator.GetByType(context)) ||
            isNotNull (MemberSignatureLikeDeclarationNavigator.GetByType(context))

        | _ -> false

    override this.Run(parenTypeUsage, _, consumer) =
        if isNull parenTypeUsage.LeftParen || isNull parenTypeUsage.RightParen then () else

        let typeUsage = parenTypeUsage.InnerTypeUsage
        let context = typeUsage.IgnoreParentParens()  

        if typeUsage :? IParenTypeUsage || applicable typeUsage && not (needsParens context typeUsage) then
            consumer.AddHighlighting(RedundantParenTypeUsageWarning(parenTypeUsage))
