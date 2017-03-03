using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.Parsing
{
  public partial class FSharpTokenType : TokenNodeType
  {
    public FSharpTokenType(string name, int index) : base(name, index)
    {
      FSharpNodeTypeIndexer.Instance.Add(this, index);
      TokenRepresentation = name;
    }

    public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
    {
      return new FSharpToken(this, buffer, startOffset, endOffset);
    }

    public override bool IsWhitespace => this == WHITESPACE || this == NEW_LINE;
    public override bool IsComment => this == COMMENT;
    public override bool IsStringLiteral => this == STRING;
    public override bool IsConstantLiteral => this == LITERAL;
    public override bool IsIdentifier => this == IDENTIFIER;
    public override bool IsKeyword => Keywords[this];
    public override string TokenRepresentation { get; }
  }
}