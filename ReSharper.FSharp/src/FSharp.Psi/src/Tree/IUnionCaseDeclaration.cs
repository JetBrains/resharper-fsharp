using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IUnionCaseDeclaration : ITypeMemberDeclaration, IFSharpDeclaration,
    IModifiersOwnerDeclaration
  {
  }

  public partial interface INestedTypeUnionCaseDeclaration
  {
    [CanBeNull] FSharpNestedTypeUnionCase NestedType { get; }
  }
}
