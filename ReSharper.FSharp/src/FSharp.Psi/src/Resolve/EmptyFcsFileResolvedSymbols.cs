using System.Collections.Generic;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class EmptyFcsFileResolvedSymbols : IFcsFileResolvedSymbols
  {
    private EmptyFcsFileResolvedSymbols()
    {
    }

    public static IFcsFileResolvedSymbols Instance = new EmptyFcsFileResolvedSymbols();

    public FSharpSymbolUse GetSymbolUse(int offset) => null;
    public FSharpSymbolUse GetSymbolDeclaration(int offset) => null;

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllDeclaredSymbols() =>
      EmptyList<FcsResolvedSymbolUse>.Instance;

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllResolvedSymbols() =>
      EmptyList<FcsResolvedSymbolUse>.Instance;

    public FSharpSymbol GetSymbol(int offset) => null;
  }
}
