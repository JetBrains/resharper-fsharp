using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  // todo: unify with LanguageService/FSharpLanguageService, it's hard to use them together now
  public interface IFSharpLanguageService
  {
    IFSharpParser  CreateParser(IDocument document);
    IFSharpElementFactory CreateElementFactory(IPsiModule psiModule);
  }
}
