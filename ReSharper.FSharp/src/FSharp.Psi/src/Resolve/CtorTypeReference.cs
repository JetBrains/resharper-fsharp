using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class CtorTypeReference : FSharpSymbolReference
  {
    public CtorTypeReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }

    public override FSharpSymbol GetFSharpSymbol() =>
      base.GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv && mfv.IsConstructor
        ? mfv.DeclaringEntity?.Value
        : null;
  }
}
