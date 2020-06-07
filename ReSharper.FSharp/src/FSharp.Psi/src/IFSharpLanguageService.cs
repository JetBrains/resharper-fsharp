using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpLanguageService
  {
    IFSharpParser CreateParser(IDocument document);
    IFSharpElementFactory CreateElementFactory(IPsiModule psiModule);
  }
}
