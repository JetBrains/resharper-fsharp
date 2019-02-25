using System.Collections.Generic;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class EmptyFileResolvedSymbols : IFSharpFileResolvedSymbols
  {
    public static IFSharpFileResolvedSymbols Instance = new EmptyFileResolvedSymbols();
    
    public FSharpSymbolUse GetSymbolUse(int offset) => null;
    public FSharpSymbol GetSymbolDeclaration(int offset) => null;

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllDeclaredSymbols() =>
      EmptyList<FSharpResolvedSymbolUse>.Instance;

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllResolvedSymbols() =>
      EmptyList<FSharpResolvedSymbolUse>.Instance;
  }
}
