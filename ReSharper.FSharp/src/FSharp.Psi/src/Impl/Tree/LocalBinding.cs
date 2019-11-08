using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LocalBinding
  {
    public TreeNodeCollection<IAttribute> AllAttributes => Attributes;
    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value) =>
      throw new System.NotImplementedException();
  }
}
