namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI

[<ElementProblemAnalyzer([| typeof<DocComment> |], HighlightingTypes = [| typeof<InvalidXmlDocPositionWarning> |])>]
type XmlDocAnalyzer() =
    inherit ElementProblemAnalyzer<DocComment>()

    override this.Run(xmlDoc, _, consumer) =
        if xmlDoc.Parent :? XmlDocBlock then () else
        consumer.AddHighlighting(InvalidXmlDocPositionWarning(xmlDoc))


[<ElementProblemAnalyzer([| typeof<XmlDocBlock> |],
                         HighlightingTypes = [| typeof<XmlDocMissingParameterWarning>
                                                typeof<XmlDocDuplicateParameterWarning>
                                                typeof<XmlDocInvalidParameterNameWarning> |])>]
type XmlDocBlockAnalyzer() =
    inherit ElementProblemAnalyzer<XmlDocBlock>()

    override this.Run(xmlDocBlock, _, consumer) =
        let xmlPsi = xmlDocBlock.GetXmlPsi()

        let paramNodes = xmlPsi.GetParameterNodes(null)
        if paramNodes.Count = 0 then () else

        let xmlDocOwner = xmlDocBlock.Parent
        if xmlDocOwner :? IFSharpTypeDeclaration then () else
        let parameters = FSharpParameterUtil.GetParametersGroupNames(xmlDocOwner)

        let parameters = parameters |> Seq.collect id

        for struct(name, parameter) in parameters do
            if name = SharedImplUtil.MISSING_DECLARATION_NAME then () else

            let parameterDocs = xmlPsi.GetParameterNodes(name)
            if parameterDocs.Count = 0 then consumer.AddHighlighting(XmlDocMissingParameterWarning(parameter))
            if parameterDocs.Count > 1 then
                for paramTag in parameterDocs do
                    let attribute = paramTag.Header.Attributes.FirstOrDefault(fun t -> t.AttributeName = "name")
                    consumer.AddHighlighting(XmlDocDuplicateParameterWarning(attribute.Value))

        for paramDoc in paramNodes do
            let attribute = paramDoc.Header.Attributes.FirstOrDefault(fun t -> t.AttributeName = "name")
            if isNull attribute then consumer.AddHighlighting(XmlDocMissingParameterNameWarning(paramDoc.Header))else

            if parameters |> Seq.exists (fun struct(name, _) -> name = attribute.UnquotedValue) then () else
            consumer.AddHighlighting(XmlDocInvalidParameterNameWarning(attribute.Value))
