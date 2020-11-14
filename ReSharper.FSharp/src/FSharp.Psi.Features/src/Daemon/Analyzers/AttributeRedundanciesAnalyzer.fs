namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi

[<ElementProblemAnalyzer(typeof<IAttribute>,
                         HighlightingTypes = [| typeof<RedundantAttributeSuffixWarning>
                                                typeof<RedundantAttributeParensWarning> |])>]
type AttributeRedundanciesAnalyzer() =
    inherit ElementProblemAnalyzer<IAttribute>()

    override x.Run(attribute, _, consumer) =
        let referenceName = attribute.ReferenceName
        if isNotNull referenceName then
            let attributeName = referenceName.ShortName
            if not (attributeName |> endsWith "Attribute" && attributeName.Length > "Attribute".Length) then () else

            let typeElement = referenceName.Reference.Resolve().DeclaredElement.As<ITypeElement>()
            if isNotNull typeElement && typeElement.ShortName = attributeName then
                consumer.AddHighlighting(RedundantAttributeSuffixWarning(attribute))

        let attributeArg = attribute.ArgExpression
        if isNotNull attributeArg && attributeArg.Expression :? IUnitExpr then
            consumer.AddHighlighting(RedundantAttributeParensWarning(attribute))
