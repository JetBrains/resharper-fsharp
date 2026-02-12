namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Syntax.PrettyNaming
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.UI.RichText

type FcsSymbolInfo(text, symbolUse: FSharpSymbolUse) =
    inherit TextualInfo(text, text)

    override this.MakeSafe(text) =
        FSharpNamingService.mangleNameIfNecessary text

    interface IFcsLookupItemInfo with
        member this.FcsSymbol = if isNotNull symbolUse then symbolUse.Symbol else Unchecked.defaultof<_>
        member this.FcsSymbolUse = symbolUse
        member this.NamespaceToOpen = [||]

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

            let icon = if isNull fcsSymbolUse then null else getIconId fcsSymbolUse.Symbol

            let info = FcsSymbolInfo(name, fcsSymbolUse, Ranges = context.Ranges)
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ -> TextualPresentation(name, info, icon))
                    .WithBehavior(fun _ -> TextualBehavior(info))
                    .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)

            item.Presentation.DisplayTypeName <-
                if isNull fcsSymbolUse then null else

                match getReturnType fcsSymbolUse.Symbol with
                | Some t -> RichText(t.Format())
                | _ -> null

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
