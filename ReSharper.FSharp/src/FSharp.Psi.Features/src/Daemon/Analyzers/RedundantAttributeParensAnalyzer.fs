namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer(typeof<IAttribute>,
                         HighlightingTypes = [| typeof<RedundantAttributeParensWarning> |])>]
type RedundantAttributeParensAnalyzer() =
    inherit ElementProblemAnalyzer<IAttribute>()

    override x.Run(attribute, _, consumer) =
        let attributeArg = attribute.ArgExpression
        if isNull attributeArg then () else

        if attributeArg.Expression :? IUnitExpr then
            consumer.AddHighlighting(RedundantAttributeParensWarning(attribute))
