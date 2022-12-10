using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IRecordFieldDeclaration : ITypeMemberDeclaration, IFSharpDeclaration,
    IAttributesOwnerDeclaration
  {
    bool IsMutable { get; }
    void SetIsMutable(bool value);
  }
}
