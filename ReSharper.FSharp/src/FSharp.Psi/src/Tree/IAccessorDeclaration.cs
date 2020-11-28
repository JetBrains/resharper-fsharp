using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IAccessorDeclaration : ITypeMemberDeclaration
  {
    IMemberDeclaration OwnerMember { get; }
    AccessorKind Kind { get; }
  }
}
