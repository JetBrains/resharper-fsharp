using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class TypeReference : FSharpSymbolReference
  {
    public TypeReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }

    public override FSharpSymbol GetFcsSymbol()
    {
      var symbol = base.GetFcsSymbol();
      if (symbol is FSharpEntity || symbol is FSharpGenericParameter)
        return symbol;

      if (symbol is FSharpMemberOrFunctionOrValue mfv)
      {
        if (mfv.IsConstructor) return mfv.DeclaringEntity?.Value;
        if (mfv.LiteralValue != null) return mfv;
      }

      return null;
    }
  }
}
