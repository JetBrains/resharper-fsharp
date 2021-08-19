using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public interface IFSharpParser : IParser
  {
    IFSharpFile ParseFSharpFile(bool noCache); // todo: conditional defines, language level
    IFSharpExpression ParseExpression(IChameleonExpression chameleonExpression, IDocument syntheticDocument = null);
  }
}
