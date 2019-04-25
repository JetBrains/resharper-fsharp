using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class InterfaceInherit
  {
    public override ITokenNode IdentifierToken =>
      BaseType?.LongIdentifier?.IdentifierToken;
  }
}
