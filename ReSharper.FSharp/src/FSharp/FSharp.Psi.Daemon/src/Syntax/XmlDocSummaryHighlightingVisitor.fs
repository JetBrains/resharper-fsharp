namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Syntax

open JetBrains.ReSharper.Daemon.CSharp.Syntax
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Psi.Xml.Tree
open JetBrains.ReSharper.Psi.Tree

type FSharpXmlDocSyntaxHighlightingVisitor(isSummary) =
    inherit XmlDocSyntaxHighlightingVisitor()

    override x.Visit(tag: IXmlTag, context: IHighlightingConsumer) =
        let header = tag.Header
        let headerRange = header.GetDocumentRange()

        let shouldHighlightTag =
            not isSummary || isNotNull header && headerRange.IsValid() && (header.IsClosed || isNotNull tag.Footer)

        shouldHighlightTag && base.Visit(tag, context)
