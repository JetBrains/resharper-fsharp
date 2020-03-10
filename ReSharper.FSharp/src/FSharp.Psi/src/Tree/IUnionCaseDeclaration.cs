using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IUnionCaseDeclaration : ITypeMemberDeclaration, IFSharpDeclaration, IModifiersOwnerDeclaration
  {
  }

  public partial interface INestedTypeUnionCaseDeclaration
  {
  }
}