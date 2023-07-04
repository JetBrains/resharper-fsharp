using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class MatchExpr
  {
    public override IType Type() =>
      ClausesEnumerable.FirstOrDefault()?.Expression?.Type() ??
      base.Type();

    public bool IsComputed
    {
      get
      {
        var tokenType = this.MatchKeyword.GetTokenType();
        return tokenType == FSharpTokenType.MATCH_BANG;
      }
    }
  }
}
