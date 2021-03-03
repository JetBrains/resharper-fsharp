using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class CtorReference : FSharpSymbolReference
  {
    public CtorReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }

    public override FSharpSymbol GetFSharpSymbol() =>
      base.GetFSharpSymbol() is FSharpMemberOrFunctionOrValue { IsConstructor: true } mfv ? mfv : null;
  }
}
