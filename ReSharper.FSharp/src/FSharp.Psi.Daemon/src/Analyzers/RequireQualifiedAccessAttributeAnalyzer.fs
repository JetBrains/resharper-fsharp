namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer(typeof<IEnumRepresentation>,
                         HighlightingTypes = [| typeof<RedundantRequireQualifiedAccessAttributeWarning> |])>]
type RequireQualifiedAccessAttributeAnalyzer() =
    inherit ElementProblemAnalyzer<IEnumRepresentation>()

    let isRqa attr =
        FSharpAttributesUtil.resolvesToType requireQualifiedAccessAttrTypeName attr

    override x.Run(repr, _, consumer) =
        let typeDecl = FSharpTypeDeclarationNavigator.GetByTypeRepresentation(repr)
        if isNull typeDecl then () else

        for attr in typeDecl.Attributes do
            if isRqa attr then
                consumer.AddHighlighting(RedundantRequireQualifiedAccessAttributeWarning(attr))
