using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class MatchExpr
  {
    public override IType Type() =>
      ClausesEnumerable.FirstOrDefault()?.Expression?.Type() ??
      base.Type();
  }
}
