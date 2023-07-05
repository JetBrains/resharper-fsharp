using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class DoExpr
  {
    public bool IsComputed => Keyword?.GetTokenType() == FSharpTokenType.DO_BANG;
  }
}
