using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  internal class FSharpNodeTypeIndexer : AbstractNodeTypeIndexer
  {
    public static readonly FSharpNodeTypeIndexer Instance = new FSharpNodeTypeIndexer();

    private FSharpNodeTypeIndexer()
    {
    }
  }
}