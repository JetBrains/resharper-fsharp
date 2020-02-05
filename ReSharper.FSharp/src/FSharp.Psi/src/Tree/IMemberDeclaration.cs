using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IMemberDeclaration : ITypeMemberDeclaration, IFSharpDeclaration, IModifiersOwner
  {
    bool IsExplicitImplementation { get; }
  }
}