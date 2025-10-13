using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ICaseFieldDeclaration : IFSharpFunctionalTypeFieldDeclaration, ITypeMemberDeclaration
  {
    bool IsNameGenerated { get; }
  }

  public interface IFSharpFunctionalTypeFieldDeclaration : IFSharpTypeOwnerDeclaration
  {
    int Index { get; }
  }
}
