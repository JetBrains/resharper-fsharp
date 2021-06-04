using System.Collections.Generic;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public interface IFcsResolvedSymbolsCache
  {
    [CanBeNull]
    FSharpSymbolUse GetSymbolUse(IPsiSourceFile sourceFile, int offset);

    [CanBeNull]
    FSharpSymbolUse GetSymbolDeclaration(IPsiSourceFile sourceFile, int offset);

    [NotNull]
    IReadOnlyList<FcsResolvedSymbolUse> GetAllDeclaredSymbols(IPsiSourceFile sourceFile);

    [NotNull]
    IReadOnlyList<FcsResolvedSymbolUse> GetAllResolvedSymbols(IPsiSourceFile sourceFile);

    [CanBeNull]
    FSharpSymbol GetSymbol(IPsiSourceFile sourceFile, int offset);
  }
}
