using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ObjExprSecondaryInterface : IInheritMember
  {
    public override ITokenNode IdentifierToken =>
      BaseType?.ReferenceName.Identifier;
  }
}
