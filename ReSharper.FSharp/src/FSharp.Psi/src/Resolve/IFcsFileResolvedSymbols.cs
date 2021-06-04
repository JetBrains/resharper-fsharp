using System.Collections.Generic;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public interface IFcsFileResolvedSymbols
  {
    [CanBeNull]
    FSharpSymbolUse GetSymbolUse(int offset);

    [CanBeNull]
    FSharpSymbolUse GetSymbolDeclaration(int offset);

    [NotNull]
    IReadOnlyList<FcsResolvedSymbolUse> GetAllDeclaredSymbols();

    [NotNull]
    IReadOnlyList<FcsResolvedSymbolUse> GetAllResolvedSymbols();

    [CanBeNull] FSharpSymbol GetSymbol(int offset);
  }
}
