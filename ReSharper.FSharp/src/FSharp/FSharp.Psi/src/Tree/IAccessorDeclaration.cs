using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IAccessorDeclaration : ITypeMemberDeclaration, IAccessRightsOwner,
    IParameterOwnerMemberDeclaration, INameIdentifierOwner
  {
    AccessorKind Kind { get; }

    /// Means the accessor has a C#-incompatible signature. todo: find a better name
    bool IsExplicit { get; }
  }
}
