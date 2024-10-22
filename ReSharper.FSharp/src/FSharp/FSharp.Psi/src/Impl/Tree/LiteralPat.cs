using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class LiteralPat
{
  public override ConstantValue ConstantValue
  {
    get
    {
      var tokenType = Literal?.GetTokenType();
      if (tokenType == null)
        return ConstantValue.NOT_COMPILE_TIME_CONSTANT;

      var psiModule = GetPsiModule();

      if (tokenType == FSharpTokenType.TRUE)
        return ConstantValue.Bool(true, psiModule);

      if (tokenType == FSharpTokenType.FALSE)
        return ConstantValue.Bool(false, psiModule);
        
      // todo: other token types
      return ConstantValue.NOT_COMPILE_TIME_CONSTANT;
    }
  }
}
