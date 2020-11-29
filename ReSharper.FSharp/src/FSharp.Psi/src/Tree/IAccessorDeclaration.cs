using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IAccessorDeclaration : ITypeMemberDeclaration
  {
    AccessorKind Kind { get; }
  }
}
