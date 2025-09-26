using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

public class TypeReference([NotNull] IFSharpReferenceOwner owner) : FSharpSymbolReference(owner)
{
  public override FSharpSymbol GetFcsSymbol()
  {
    var symbol = base.GetFcsSymbol();
    return symbol switch
    {
      FSharpEntity or FSharpGenericParameter => symbol,
      FSharpMemberOrFunctionOrValue { IsConstructor: true, ApparentEnclosingEntity.Value: { } entity } => entity,
      FSharpMemberOrFunctionOrValue { LiteralValue: not null } mfv => mfv,
      _ => null
    };
  }
}
