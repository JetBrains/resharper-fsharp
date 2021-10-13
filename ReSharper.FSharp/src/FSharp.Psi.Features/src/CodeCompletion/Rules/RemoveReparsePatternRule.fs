namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.Util.Extension

[<Language(typeof<FSharpLanguage>)>]
type RemoveReparsePatternRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion && not context.IsQualified &&

        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        isNotNull reference &&

        let refPat = ReferencePatNavigator.GetByReferenceName(reference.GetElement().As())
        isNotNull refPat && isNotNull refPat.NameIdentifier

    override this.TransformItems(context, collector) =
        let reference = context.ReparsedContext.Reference :?> FSharpSymbolReference
        let referencePat = ReferencePatNavigator.GetByReferenceName(reference.GetElement() :?> _)

        let reparsedName = referencePat.NameIdentifier.Name
        let name = reparsedName.SubstringBeforeLast(FSharpCompletionUtil.DummyIdentifier)
        if reparsedName.Length = name.Length then () else

        let treeStartOffset = referencePat.GetTreeStartOffset()
        collector.RemoveWhere(fun item ->
            let lookupItem = item.As<FcsLookupItem>()
            isNotNull lookupItem && lookupItem.Text = name &&

            match lookupItem.FcsSymbol with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                not mfv.IsModuleValueOrMember &&
                getTreeStartOffset context.BasicContext.Document mfv.DeclarationLocation = treeStartOffset
            | _ -> false)
