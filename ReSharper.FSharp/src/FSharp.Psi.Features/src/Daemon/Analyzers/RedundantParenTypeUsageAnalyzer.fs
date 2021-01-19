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

    let needsParens (context: ITypeUsage) (typeUsage: ITypeUsage): bool =
        let parentTypeUsage = context.Parent.As<ITypeUsage>()
        if isNotNull parentTypeUsage && not (applicable parentTypeUsage) then true else

        match typeUsage with
        | :? ITupleTypeUsage ->
            isNotNull (TupleTypeUsageNavigator.GetByItem(context)) ||
            isNotNull (ArrayTypeUsageNavigator.GetByType(context)) ||
            isNotNull (PostfixAppTypeArgumentListNavigator.GetByType(context))

        | :? IFunctionTypeUsage ->
            isNotNull (TupleTypeUsageNavigator.GetByItem(context)) ||
            isNotNull (FunctionTypeUsageNavigator.GetByArgumentTypeUsage(context)) ||
            isNotNull (ArrayTypeUsageNavigator.GetByType(context)) ||
            isNotNull (PostfixAppTypeArgumentListNavigator.GetByType(context)) ||
            isNotNull (IsInstPatNavigator.GetByType(context))

        | _ -> false

    override this.Run(parenTypeUsage, _, consumer) =
        if isNull parenTypeUsage.LeftParen || isNull parenTypeUsage.RightParen then () else

        let innerTypeUsage = parenTypeUsage.InnerTypeUsage
        let context = innerTypeUsage.IgnoreParentParens()  

        if innerTypeUsage :? IParenExpr || applicable innerTypeUsage && not (needsParens context innerTypeUsage) then
            consumer.AddHighlighting(RedundantParenTypeUsageWarning(parenTypeUsage))
