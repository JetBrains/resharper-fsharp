using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IMemberSignatureOrDeclaration : ITypeMemberDeclaration, IFSharpDeclaration,
    IModifiersOwnerDeclaration
  {
    bool IsIndexer { get; }
  }
}
