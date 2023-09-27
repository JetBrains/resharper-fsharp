using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IUnionCaseLikeDeclaration : ITypeMemberDeclaration, IFSharpDeclaration,
    IModifiersOwnerDeclaration
  {
    bool HasFields { get; }
    [CanBeNull] FSharpUnionCaseClass NestedType { get; }
  }
}
