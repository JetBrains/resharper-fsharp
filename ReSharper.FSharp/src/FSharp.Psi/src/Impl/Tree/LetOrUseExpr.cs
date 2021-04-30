using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LetOrUseExpr
  {
    public IBinding FirstBinding => BindingsEnumerable.FirstOrDefault();
    public ITokenNode BindingKeyword => FirstBinding?.BindingKeyword;

    public bool IsRecursive => FirstBinding?.RecKeyword != null;

    public bool IsUse => 
      BindingKeyword?.GetTokenType() is var tokenType && 
      (tokenType == FSharpTokenType.USE || tokenType == FSharpTokenType.USE_BANG);

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      BindingKeyword.NotNull().AddTokenAfter(FSharpTokenType.REC);
    }

    public override IType Type() => 
      InExpression?.Type() ?? base.Type();
  }
}
