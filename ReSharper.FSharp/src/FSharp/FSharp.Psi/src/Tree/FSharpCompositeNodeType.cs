using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public abstract class FSharpCompositeNodeType : CompositeNodeType
  {
    protected FSharpCompositeNodeType(string s, int index, NodeTypeFlags flags) : base(s, index, flags) =>
      FSharpNodeTypeIndexer.Instance.Add(this, index);
  }
}
