using System.Collections.Generic;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Diagnostics;
using FSharp.Compiler.Symbols;
using FSharp.Compiler.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class EmptyFcsFileCapturedInfo : IFcsFileCapturedInfo
  {
    private EmptyFcsFileCapturedInfo()
    {
    }

    public static IFcsFileCapturedInfo Instance = new EmptyFcsFileCapturedInfo();

    public FSharpSymbolUse GetSymbolUse(int offset) => null;
    public FSharpSymbolUse GetSymbolDeclaration(int offset) => null;

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllDeclaredSymbols() =>
      EmptyList<FcsResolvedSymbolUse>.Instance;

    public IReadOnlyList<FcsResolvedSymbolUse> GetAllResolvedSymbols() =>
      EmptyList<FcsResolvedSymbolUse>.Instance;

    public FSharpSymbol GetSymbol(int offset) => null;
    public FSharpDiagnostic GetDiagnostic(Position offset) => null;

    public void SetCachedDiagnostics(IDictionary<Position, FSharpDiagnostic> diagnostics)
    {
    }
  }
}
