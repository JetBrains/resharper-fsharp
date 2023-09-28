using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ICaseFieldDeclaration : IFSharpDeclaration, ITypeMemberDeclaration
  {
    bool IsNameGenerated { get; }
  }
}
