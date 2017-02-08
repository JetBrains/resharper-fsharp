using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Psi.FSharp
{
  class FSharpNodeTypeIndexer : AbstractNodeTypeIndexer
  {
    public static readonly FSharpNodeTypeIndexer Instance = new FSharpNodeTypeIndexer();

    private FSharpNodeTypeIndexer()
    {
    }
  }
}