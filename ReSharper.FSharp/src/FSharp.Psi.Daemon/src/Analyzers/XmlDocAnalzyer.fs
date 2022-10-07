namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open System.Collections.Generic
open JetBrains.ReSharper.Daemon.Xml.Highlightings
open JetBrains.ReSharper.Daemon.Xml.Stages
open JetBrains.ReSharper.Daemon.Xml.Stages.Analysis
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Xml.Tree
open JetBrains.ReSharper.Psi.Xml.XmlDocComments

[<ElementProblemAnalyzer([| typeof<DocComment> |], HighlightingTypes = [| typeof<InvalidXmlDocPositionWarning> |])>]
type XmlDocAnalyzer() =
    inherit ElementProblemAnalyzer<DocComment>()

    override this.Run(xmlDoc, _, consumer) =
        if xmlDoc.Parent :? XmlDocBlock then () else
        consumer.AddHighlighting(InvalidXmlDocPositionWarning(xmlDoc))


[<ElementProblemAnalyzer([| typeof<XmlDocBlock> |],
                         HighlightingTypes = [| typeof<XmlDocCommentSyntaxWarning>
                                                typeof<XmlDocMissingParameterWarning>
                                                typeof<XmlDocDuplicateParameterWarning>
                                                typeof<XmlDocInvalidParameterNameWarning> |])>]
type XmlDocBlockAnalyzer(xmlAnalysisManager: XmlAnalysisManager) =
    inherit ElementProblemAnalyzer<XmlDocBlock>()

    let getNameAttribute (paramTag: IXmlTag) =
        paramTag.Header.Attributes.FirstOrDefault(fun t -> t.AttributeName = "name")

    let checkXmlHighlighting (highlighting: IHighlighting) =
        match highlighting with
        | :? XmlOnlyOneTagAllowedAtRootLevelHighlighting
        | :? XmlNoRootTagDefinedHighlighting
        | :? XmlTextIsNotAllowedAtRootHighlighting -> false
        | _ -> true
    
    let checkXmlSyntax (xmlPsi: IDocCommentXmlPsi) (data: ElementProblemAnalyzerData) (consumer: IHighlightingConsumer) =
        let daemonProcess = data.TryGetDaemonProcess()
        if isNull daemonProcess then () else

        let xmlFile = xmlPsi.XmlFile
        let analyses = List<XmlAnalysis>()

        for provider in xmlAnalysisManager.Providers do
        for analysis in provider.GetAnalyses(xmlFile, daemonProcess, data.SettingsStore) do
            analyses.Add(analysis)

        let xmlConsumer = DefaultHighlightingConsumer(data.SourceFile)
        let xmlAnalysisProcess = XmlAnalysisStageProcess(xmlFile, analyses, daemonProcess, xmlConsumer)

        xmlAnalysisProcess.Execute(fun result ->
        for highlighting in result.Highlightings do
            if  not (checkXmlHighlighting highlighting.Highlighting) then () else

            let warning = XmlDocCommentSyntaxWarning(highlighting.Highlighting, highlighting.Range)
            consumer.AddHighlighting(warning, highlighting.Range))
    
    let checkParameters (xmlPsi: IDocCommentXmlPsi) (xmlDocOwner: ITreeNode) (consumer: IHighlightingConsumer) =
        let paramNodes = xmlPsi.GetParameterNodes(null)
        if paramNodes.Count = 0 then () else

        if xmlDocOwner :? IFSharpTypeDeclaration then () else
        let parameters = FSharpParameterUtil.GetParametersGroupNames(xmlDocOwner)

        let parameters = parameters |> Seq.collect id

        for struct(name, parameter) in parameters do
            if name = SharedImplUtil.MISSING_DECLARATION_NAME then () else

            let parameterDocs = xmlPsi.GetParameterNodes(name)

            if parameterDocs.Count = 0 then
                consumer.AddHighlighting(XmlDocMissingParameterWarning(parameter))

            elif parameterDocs.Count > 1 then
                for paramTag in parameterDocs do
                    let attribute = getNameAttribute paramTag
                    consumer.AddHighlighting(XmlDocDuplicateParameterWarning(attribute.Value))

        for paramDoc in paramNodes do
            let attribute = getNameAttribute paramDoc

            if isNull attribute then
                consumer.AddHighlighting(XmlDocMissingParameterNameWarning(paramDoc.Header))else

            if parameters |> Seq.exists (fun struct(name, _) -> name = attribute.UnquotedValue) then () else
            consumer.AddHighlighting(XmlDocInvalidParameterNameWarning(attribute.Value))

    
    override this.Run(xmlDocBlock, data, consumer) =
        let xmlPsi = xmlDocBlock.GetXmlPsi()
        let xmlDocOwner = xmlDocBlock.Parent

        checkXmlSyntax xmlPsi data consumer
        checkParameters xmlPsi xmlDocOwner consumer    
