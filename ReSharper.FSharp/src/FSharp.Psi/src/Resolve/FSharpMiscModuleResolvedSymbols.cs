using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  internal class FSharpMiscModuleResolvedSymbols : IFSharpModuleResolvedSymbols
  {
    public static readonly FSharpMiscModuleResolvedSymbols Instance = new FSharpMiscModuleResolvedSymbols();

    public void Invalidate()
    {
    }

    public void Invalidate(IPsiSourceFile sourceFile)
    {
    }

    public IFcsFileResolvedSymbols GetResolvedSymbols(IPsiSourceFile sourceFile) =>
      EmptyFcsFileResolvedSymbols.Instance;
  }
}
