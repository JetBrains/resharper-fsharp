namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<ElementProblemAnalyzer(typeof<IAttribute>,
                         HighlightingTypes = [| typeof<RedundantAttributeSuffixWarning> |])>]
type RedundantAttributeSuffixAnalyzer() =
    inherit ElementProblemAnalyzer<IAttribute>()

    override x.Run(attribute, _, consumer) =
        let attributeName = attribute.ReferenceName.Identifier.Name
        if not (attributeName.EndsWith("Attribute")) then () else

        let attributeTypeElement = attribute.ReferenceName.Reference.Resolve().DeclaredElement.As<ITypeElement>()
        if isNull attributeTypeElement then () else

        let attributeTypeName = attributeTypeElement.GetClrName().ShortName
        if attributeTypeName = attributeName then
            consumer.AddHighlighting(RedundantAttributeSuffixWarning(attribute))
