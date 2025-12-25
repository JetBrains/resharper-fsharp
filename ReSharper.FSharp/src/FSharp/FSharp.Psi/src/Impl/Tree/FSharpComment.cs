using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
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
      new(FSharpTokenType.LINE_COMMENT, "//" + text);
  }

  public class DocComment : FSharpComment
  {
    public DocComment([NotNull] TokenNodeType nodeType, [NotNull] string text) : base(nodeType, text)
    {
    }
  }

  public partial class XmlDocBlock : FSharpCompositeElement
  {
    public override bool IsFiltered() => true;
    public override NodeType NodeType => ElementType.DOC_COMMENT_BLOCK;
  }
  
  public class FSharpWarningDirective : FSharpCompositeElement
  {
    public override bool IsFiltered() => true;
    public override NodeType NodeType => ElementType.WARNING_DIRECTIVE;
  }

  partial class ElementType
  {
    public static readonly CompositeNodeType DOC_COMMENT_BLOCK = new DocCommentBlockNodeType();
    public const int DOC_COMMENT_BLOCK_INDEX = 1900;

    private sealed class DocCommentBlockNodeType : FSharpCompositeNodeType
    {
      public DocCommentBlockNodeType() : base("XML_DOC_BLOCK", DOC_COMMENT_BLOCK_INDEX, typeof(XmlDocBlock))
      {
      }

      public override CompositeElement Create() => new XmlDocBlock();
    }

    public static readonly CompositeNodeType WARNING_DIRECTIVE = new FSharpWarningDirectiveNodeType();
    public const int WARNING_DIRECTIVE_INDEX = 1901;

    private sealed class FSharpWarningDirectiveNodeType : FSharpCompositeNodeType
    {
      public FSharpWarningDirectiveNodeType() : base("WARNING_DIRECTIVE", WARNING_DIRECTIVE_INDEX, typeof(FSharpWarningDirective))
      {
      }

      public override CompositeElement Create() => new FSharpWarningDirective();
    }
  }
}
