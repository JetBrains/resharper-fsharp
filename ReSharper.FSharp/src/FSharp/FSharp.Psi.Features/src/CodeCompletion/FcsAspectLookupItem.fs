namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util

type FcsSymbolInfo(text, symbol: FSharpSymbol, isFromComputationExpression: bool, context: FSharpCodeCompletionContext) =
    inherit TextualInfo(text, text)

    new (text, symbol, context) =
        FcsSymbolInfo(text, symbol, false, context)

    override this.MakeSafe(text) =
        FSharpNamingService.mangleNameIfNecessary text

    member this.Context = context

    member this.FcsSymbol =
        if isNotNull symbol then symbol else Unchecked.defaultof<_>

    member this.TypeText =
        if isNull symbol then null else

        match getReturnType symbol with
        | Some t -> t.Format()
        | _ -> null

    interface IFcsLookupItemInfo with
        member this.FcsSymbol = this.FcsSymbol
        member this.IsFromComputationExpression = isFromComputationExpression

    interface IDescriptionProvidingLookupItem with
        member this.GetDescription() =
            match context.GetCheckResults() with
            | None -> null
            | Some(checkResults) ->

            let _, range = context.ReparsedContext.TreeNode.TryGetFcsRange()
            let toolTipText = checkResults.GetDescription(symbol, [], false, range)

            toolTipText
            |> FcsLookupCandidate.getOverloads
            |> List.tryHead
            |> Option.map (FcsLookupCandidate.getDescription context.XmlDocService context.PsiModule)
            |> Option.defaultValue null


type FcsSymbolPresentation(info: FcsSymbolInfo, emphasize, icon) =
    inherit TextPresentation<FcsSymbolInfo>(info, info.TypeText, emphasize, icon)

    new (info, icon) =
        FcsSymbolPresentation(info, false, icon)

    override this.GetDisplayName() =
        let name = base.GetDisplayName()

        if isObsolete info.FcsSymbol then
            LookupUtil.StrikeOut(name)

        name
