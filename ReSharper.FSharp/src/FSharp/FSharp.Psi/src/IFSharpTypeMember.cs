using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpTypeMember : IFSharpTypeParametersOwner, ITypeMember
  {
    [CanBeNull] FSharpSymbol Symbol { get; }
    [CanBeNull] FSharpSymbolUse SymbolUse { get; }

    bool IsVisibleFromFSharp { get; }
    bool CanNavigateTo { get; }
  }
}
