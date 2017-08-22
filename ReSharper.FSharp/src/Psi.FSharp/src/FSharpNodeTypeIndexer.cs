using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  class FSharpNodeTypeIndexer : AbstractNodeTypeIndexer
  {
    public static readonly FSharpNodeTypeIndexer Instance = new FSharpNodeTypeIndexer();

    private FSharpNodeTypeIndexer()
    {
    }
  }
}