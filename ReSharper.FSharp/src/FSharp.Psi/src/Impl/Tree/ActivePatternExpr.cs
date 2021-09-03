using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ActivePatternExpr
  {
    public override IFSharpIdentifier FSharpIdentifier => ActivePatternId;

    protected override FSharpSymbolReference CreateReference() =>
      new FSharpSymbolReference(this);
  }
}
