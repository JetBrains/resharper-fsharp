using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.Parsing
{
  public class FSharpTokenNodeType : TokenNodeType
  {
    public FSharpTokenNodeType(string name, int index) : base(name, index)
    {
      FSharpNodeTypeIndexer.Instance.Add(this, index);
    }

    public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
    {
      return new FSharpToken(this, buffer, startOffset, endOffset);
    }

    public override bool IsWhitespace => this == FSharpTokenType.WHITESPACE || this == FSharpTokenType.NEW_LINE;
    public override bool IsComment => this == FSharpTokenType.COMMENT;
    public override bool IsStringLiteral => this == FSharpTokenType.STRING;
    public override bool IsConstantLiteral => this == FSharpTokenType.LITERAL;
    public override bool IsIdentifier => this == FSharpTokenType.IDENTIFIER;
    public override bool IsKeyword => this == FSharpTokenType.KEYWORD;
    public override string TokenRepresentation => "F# token";
  }
}