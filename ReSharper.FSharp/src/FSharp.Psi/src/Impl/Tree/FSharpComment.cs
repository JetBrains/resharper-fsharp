using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public enum CommentType : byte
  {
    EndOfLineComment,
    MultilineComment,
    DocComment
  }

  public class FSharpComment : FSharpToken, ICommentNode
  {
    public FSharpComment(NodeType nodeType, string text) : base(nodeType, text)
    {
    }

    public override bool IsFiltered() => true;

    public CommentType CommentType
    {
      get
      {
        if (NodeType == FSharpTokenType.BLOCK_COMMENT)
          return CommentType.MultilineComment;

        var text = GetText();
        if (text.StartsWith("///") && !text.StartsWith("////")) // todo: remove duplication with comment line actions
          return CommentType.DocComment;

        return CommentType.EndOfLineComment;
      }
    }

    public TreeTextRange GetCommentRange()
    {
      var startOffset = GetTreeStartOffset();
      switch (CommentType)
      {
        case CommentType.EndOfLineComment:
          return new TreeTextRange(startOffset + 2, startOffset + GetTextLength());

        case CommentType.DocComment:
          return new TreeTextRange(startOffset + 3, startOffset + GetTextLength());

        case CommentType.MultilineComment:
        {
          var text = GetText();
          var length = text.Length - (text.EndsWith("*)") ? 4 : 2);
          return length > 0
            ? new TreeTextRange(startOffset + 2, startOffset + 2 + length)
            : TreeTextRange.InvalidRange;
        }
      }

      return TreeTextRange.InvalidRange;
    }

    public string CommentText
    {
      get
      {
        var text = GetText();
        switch (CommentType)
        {
          case CommentType.EndOfLineComment:
            return text.Substring(2);

          case CommentType.DocComment:
            return text.Substring(3);

          case CommentType.MultilineComment:
            var length = text.Length - (text.EndsWith("*)") ? 4 : 2);
            return length <= 0
              ? string.Empty
              : text.Substring(2, length);
        }

        return string.Empty;
      }
    }

    public static FSharpComment CreateLineComment(string text) =>
      new FSharpComment(FSharpTokenType.LINE_COMMENT, "//" + text);
  }

  public class DocComment : FSharpComment
  {
    public DocComment([NotNull] TokenNodeType nodeType, [NotNull] string text) : base(nodeType, text)
    {
    }
  }

  public class XmlDocBlock : FSharpCompositeElement
  {
    public override bool IsFiltered() => true;
    public override NodeType NodeType => FSharpTokenType.XML_DOC_BLOCK;
  }
}
