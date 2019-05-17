using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public interface IFSharpParser : IParser
  {
    ISynExpr ParseExpression(IChameleonExpression chameleonExpression);
  }
}
