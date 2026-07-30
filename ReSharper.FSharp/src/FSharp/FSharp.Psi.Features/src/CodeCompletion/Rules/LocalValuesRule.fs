namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.Syntax.PrettyNaming
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type LocalValuesRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion &&

        let node = context.ReparsedContext.TreeNode
        isNotNull node &&

        let refExpr = node.Parent.As<IReferenceExpr>()
        isNotNull refExpr && not refExpr.IsQualified && refExpr.Identifier == node

    override this.AddLookupItems(context, collector) =
        let values =
            let treeNode = context.ReparsedContext.TreeNode
            context.GetOrCreateDataUnderLock(LocalValuesUtil.valuesKey, treeNode, LocalValuesUtil.getLocalValues)

        for KeyValue(name, (_, fcsSymbolUse)) in values do
            if IsOperatorDisplayName name then () else

            let symbol = if isNotNull fcsSymbolUse then fcsSymbolUse.Symbol else Unchecked.defaultof<_>
            let icon = getIconId symbol

            let info = FcsSymbolInfo(name, symbol, context, Ranges = context.Ranges)
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ -> FcsSymbolPresentation(info, icon))
                    .WithBehavior(fun _ -> TextualBehavior(info))
                    .WithMatcher(LookupItemMatcher.Literal)

            collector.Add(item)

        false

    override this.TransformItems(context, collector) =
        let values = context.GetData(LocalValuesUtil.valuesKey)
        if isNull values then () else

        collector.RemoveWhere(fun item ->
            let fcsLookupItem = item.As<FcsLookupItem>()
            isNotNull fcsLookupItem &&

            values.ContainsKey(fcsLookupItem.DisplayName)
        )
