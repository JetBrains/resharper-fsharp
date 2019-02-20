using JetBrains.Annotations;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class FSharpFileResolvedSymbols
  {
    public static readonly FSharpFileResolvedSymbols Empty = new FSharpFileResolvedSymbols();

    public FSharpFileResolvedSymbols(int symbolUsesCount = 0)
    {
      Declarations = new CompactMap<int, FSharpResolvedSymbolUse>(symbolUsesCount / 4);
      Uses = new CompactMap<int, FSharpResolvedSymbolUse>(symbolUsesCount);
    }

    [NotNull] public CompactMap<int, FSharpResolvedSymbolUse> Declarations;
    [NotNull] public CompactMap<int, FSharpResolvedSymbolUse> Uses;
  }
}
