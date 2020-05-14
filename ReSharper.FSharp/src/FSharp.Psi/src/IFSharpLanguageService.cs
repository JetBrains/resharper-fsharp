using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpLanguageService
  {
    IFSharpArgumentsOwner GetArgumentsOwner(IFSharpExpression fsExpr);
    IParameter GetMatchingParameter(IFSharpExpression fsExpr);
    IParameter GetNamedArg(IFSharpExpression fsExpr);
  }
}
