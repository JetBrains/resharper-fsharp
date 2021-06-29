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

      if (base.GetFcsSymbol() is FSharpMemberOrFunctionOrValue { IsConstructor: true } mfv)
        return mfv.DeclaringEntity?.Value;

      return null;
    }
  }
}
