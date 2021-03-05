using System.Collections.Generic;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class EmptyFileResolvedSymbols : IFSharpFileResolvedSymbols
  {
    public static IFSharpFileResolvedSymbols Instance = new EmptyFileResolvedSymbols();

    public FSharpSymbolUse GetSymbolUse(int offset) => null;
    public FSharpSymbolUse GetSymbolDeclaration(int offset) => null;

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllDeclaredSymbols() =>
      EmptyList<FSharpResolvedSymbolUse>.Instance;

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllResolvedSymbols() =>
      EmptyList<FSharpResolvedSymbolUse>.Instance;

    public FSharpSymbol GetSymbol(int offset) => null;
  }
}
