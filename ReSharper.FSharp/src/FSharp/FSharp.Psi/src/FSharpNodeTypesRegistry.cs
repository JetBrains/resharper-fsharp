using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  internal sealed class FSharpNodeTypesRegistry : NodeTypesRegistry
  {
    public static readonly FSharpNodeTypesRegistry Instance = new();

    private FSharpNodeTypesRegistry()
    {
    }
  }
}
