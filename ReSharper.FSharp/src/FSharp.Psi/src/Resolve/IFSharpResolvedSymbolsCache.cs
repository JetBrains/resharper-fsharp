using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public interface IFSharpResolvedSymbolsCache
  {
    [CanBeNull]
    FSharpSymbolUse GetSymbolUse(IPsiSourceFile sourceFile, int offset);

    [CanBeNull]
    FSharpSymbol GetSymbolDeclaration(IPsiSourceFile sourceFile, int offset);

    [NotNull]
    IReadOnlyList<FSharpResolvedSymbolUse> GetAllDeclaredSymbols(IPsiSourceFile sourceFile);

    [NotNull]
    IReadOnlyList<FSharpResolvedSymbolUse> GetAllResolvedSymbols(IPsiSourceFile sourceFile);
  }
}
