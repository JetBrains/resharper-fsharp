using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IBindingLikeDeclaration
  {
    TreeNodeCollection<IAttribute> AllAttributes { get; }

    bool IsMutable { get; }
    void SetIsMutable(bool value);
  }
}
