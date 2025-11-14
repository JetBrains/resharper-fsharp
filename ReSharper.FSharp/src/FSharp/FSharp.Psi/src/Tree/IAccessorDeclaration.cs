using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IAccessorDeclaration : ITypeMemberDeclaration, IAccessRightsOwner,
    IParameterOwnerMemberDeclaration, INameIdentifierOwner
  {
    string AccessorName { get; }
    AccessorKind Kind { get; }
    bool IsIndexerLike { get; }
    bool IsAutoPropertyAccessor { get; }
  }
}
