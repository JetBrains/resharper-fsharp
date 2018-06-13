using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class FSharpDeadCodeToken : FSharpToken
  {
    public FSharpDeadCodeToken(NodeType nodeType, string text) : base(nodeType, text)
    {
    }
  }
}