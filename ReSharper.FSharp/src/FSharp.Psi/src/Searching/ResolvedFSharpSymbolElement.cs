using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Searching
{
  /// <summary>
  /// Produced during reference resolve and is used to find usages when symbol generally cannot be resolved to R# element.
  /// When a symbol is declared in source code, we try to replace the element of this type with declared element during GoToDeclaration action.
  /// </summary>
  public interface IFSharpSymbolElement : IDeclaredElement
  {
    FSharpSymbol Symbol { get; }
    IPsiModule Module { get; }
  }

  public class ResolvedFSharpSymbolElement : IFSharpSymbolElement
  {
    public ResolvedFSharpSymbolElement(FSharpSymbol symbol, FSharpIdentifierToken referenceOwnerToken)
    {
      Symbol = symbol;
      Module = referenceOwnerToken.GetPsiModule();
    }

    public string ShortName => Symbol.DisplayName;

    public FSharpSymbol Symbol { get; }
    public IPsiModule Module { get; }

    public IPsiServices GetPsiServices() => Module.GetPsiServices();

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile) => false;
    public IList<IDeclaration> GetDeclarations() => EmptyList<IDeclaration>.Instance;
    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) => EmptyList<IDeclaration>.Instance;
    public HybridCollection<IPsiSourceFile> GetSourceFiles() => HybridCollection<IPsiSourceFile>.Empty;

    public DeclaredElementType GetElementType() =>
      FSharpDeclaredElementType.ActivePatternCase; // todo: check this

    public XmlNode GetXMLDoc(bool inherit) => null;
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null;

    public bool IsValid() => true;  // todo: check this

    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;
    public bool CaseSensitiveName => true;
    public bool IsSynthetic() => false;
  }
}
