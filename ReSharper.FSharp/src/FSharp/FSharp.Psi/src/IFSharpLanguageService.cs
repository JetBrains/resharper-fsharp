using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpLanguageService
  {
    IFSharpParser CreateParser(IDocument document, IPsiSourceFile sourceFile, string overrideExtension = null);
    IFSharpElementFactory CreateElementFactory(ITreeNode context, string overrideExtension = null);
  }
}
