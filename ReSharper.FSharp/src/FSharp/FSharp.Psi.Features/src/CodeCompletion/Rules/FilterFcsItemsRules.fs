namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FilterFcsPatternRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let isReferencePat (referenceOwner: IFSharpReferenceOwner) =
        isNotNull (ReferencePatNavigator.GetByReferenceName(referenceOwner.As()))

    let isTypeLikePat (referenceOwner: IFSharpReferenceOwner) =
        let typeUsage = NamedTypeUsageNavigator.GetByReferenceName(referenceOwner.As())
        isNotNull (TypedLikePatNavigator.GetByTypeUsage(typeUsage))

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion && not context.IsQualified &&

        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        isNotNull reference &&

        let referenceOwner = reference.GetElement()
        (isReferencePat referenceOwner || isTypeLikePat referenceOwner)

    override this.TransformItems(_, collector) =
        collector.RemoveWhere(fun item ->
            let lookupItem = item.As<FcsLookupItem>()
            isNotNull lookupItem &&

            match lookupItem.FcsSymbol with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                mfv.IsModuleValueOrMember && Option.isNone mfv.LiteralValue
            | _ -> false
        )



[<Language(typeof<FSharpLanguage>)>]
type FilterFcsExpressionRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion && not context.IsQualified

    override this.TransformItems(_, collector) =
        collector.RemoveWhere(fun item ->
            let lookupItem = item.As<FcsLookupItem>()
            if isNull lookupItem then false else

            match lookupItem.Text with
            | "``base``" ->
                match lookupItem.FcsSymbol with
                | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.IsBaseValue
                | _ -> false
            | _ -> false)
