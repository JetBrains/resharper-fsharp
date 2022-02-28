namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree

[<ElementProblemAnalyzer([| typeof<DocComment> |], HighlightingTypes = [| typeof<InvalidXmlDocPositionWarning> |])>]
type XmlDocAnalyzer() =
    inherit ElementProblemAnalyzer<DocComment>()

    override this.Run(xmlDoc, _, consumer) =
        if xmlDoc.Parent :? XmlDocBlock then () else
        consumer.AddHighlighting(InvalidXmlDocPositionWarning(xmlDoc))
