using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class SequentialExpr
  {
    public override IType Type() =>
      ExpressionsEnumerable.LastOrDefault()?.Type() ??
      base.Type();
  }
}
