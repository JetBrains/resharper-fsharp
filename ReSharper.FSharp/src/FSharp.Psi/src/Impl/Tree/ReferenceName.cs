namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeReferenceName
  {
    public FSharpIdentifierToken Identifier => IdentifierInternal as FSharpIdentifierToken;
  }

  internal partial class ExpressionReferenceName
  {
    public FSharpIdentifierToken Identifier => IdentifierInternal as FSharpIdentifierToken;
  }
}
