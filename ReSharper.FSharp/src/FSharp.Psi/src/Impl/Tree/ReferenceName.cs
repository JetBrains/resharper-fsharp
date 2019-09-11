using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeReferenceName : IPreventsChildResolve
  {
    public FSharpIdentifierToken Identifier => IdentifierInternal as FSharpIdentifierToken;
    public override ITokenNode IdentifierToken => Identifier;
  }

  internal partial class ExpressionReferenceName
  {
    public FSharpIdentifierToken Identifier => IdentifierInternal as FSharpIdentifierToken;
  }
}
