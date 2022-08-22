namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open JetBrains.ReSharper.Daemon.CSharp.Syntax
open JetBrains.ReSharper.Daemon.Syntax
open JetBrains.ReSharper.Daemon.SyntaxHighlighting
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi

type FSharpSyntaxHighlighting() =
    inherit DefaultSyntaxHighlighting()

    static let strings =
        FSharpTokenType.InterpolatedStrings.Union(FSharpTokenType.Strings)

    override x.IsBlockComment(tokenType) = tokenType == FSharpTokenType.BLOCK_COMMENT
    override x.IsLineComment(tokenType) = tokenType == FSharpTokenType.LINE_COMMENT
    override x.IsString(tokenType) = strings[tokenType]

    override x.BlockCommentAttributeId = FSharpHighlightingAttributeIds.BlockComment
    override x.LineCommentAttributeId = FSharpHighlightingAttributeIds.LineComment
    override x.StringAttributeId = FSharpHighlightingAttributeIds.String
    override x.PreprocessorAttributeId = FSharpHighlightingAttributeIds.PreprocessorKeyword
    override x.KeywordAttributeId = FSharpHighlightingAttributeIds.Keyword
    override x.NumberAttributeId = FSharpHighlightingAttributeIds.Number


type FSharpSyntaxHighlightingProcessor() =
    inherit SyntaxHighlightingProcessor()

    let highlighting = FSharpSyntaxHighlighting()

    override x.GetAttributeId(tokenType) = highlighting.GetAttributeId(tokenType)
    override x.ProcessBeforeInterior(element, context) =
        match element with
        | :? IFSharpDocCommentBlock as block ->
            let xmlFile = block.GetXmlPsi().XmlFile
            xmlFile.ProcessDescendants(XmlDocSyntaxHighlightingVisitor(), context); //TODO: F# implementation
        | _ -> ()


[<Language(typeof<FSharpLanguage>)>]
type FSharpSyntaxHighlightingManager() =
    inherit SyntaxHighlightingManager()

    override x.CreateProcessor() = FSharpSyntaxHighlightingProcessor() :> _
