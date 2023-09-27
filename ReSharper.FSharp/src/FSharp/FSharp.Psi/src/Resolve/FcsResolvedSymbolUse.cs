using FSharp.Compiler.CodeAnalysis;
using JetBrains.Annotations;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class FcsResolvedSymbolUse
  {
    [NotNull]
    public FSharpSymbolUse SymbolUse { get; }

    public TextRange Range { get; }

    public FcsResolvedSymbolUse([NotNull] FSharpSymbolUse symbolUse, TextRange range)
    {
      SymbolUse = symbolUse;
      Range = range;
    }

    public override string ToString() =>
      SymbolUse.ToString();
  }
}
