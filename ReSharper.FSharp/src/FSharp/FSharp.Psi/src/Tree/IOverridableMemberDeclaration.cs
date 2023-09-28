using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IOverridableMemberDeclaration : ITypeMemberDeclaration, IFSharpDeclaration,
    IModifiersOwnerDeclaration
  {
    bool IsExplicitImplementation { get; }
  }
}
