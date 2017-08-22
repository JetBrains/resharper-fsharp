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
    private readonly IPsiServices myPsiServices;

    public ResolvedFSharpSymbolElement(FSharpSymbol symbol, FSharpIdentifierToken referenceOwnerToken)
    {
      Symbol = symbol;
      Module = referenceOwnerToken.GetPsiModule();
      myPsiServices = referenceOwnerToken.GetPsiServices();
    }
    
    public FSharpSymbol Symbol { get; }
    public IPsiModule Module { get; }

    public IPsiServices GetPsiServices()
    {
      return myPsiServices;
    }

    public IList<IDeclaration> GetDeclarations()
    {
      return EmptyList<IDeclaration>.Instance;
    }

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return EmptyList<IDeclaration>.Instance;
    }

    public DeclaredElementType GetElementType()
    {
      return FSharpDeclaredElementType.ActivePatternCase;
    }

    public XmlNode GetXMLDoc(bool inherit)
    {
      return null;
    }

    public XmlNode GetXMLDescriptionSummary(bool inherit)
    {
      return null;
    }

    public bool IsValid()
    {
      return true;
    }

    public bool IsSynthetic()
    {
      return false;
    }

    public HybridCollection<IPsiSourceFile> GetSourceFiles()
    {
      return HybridCollection<IPsiSourceFile>.Empty;
    }

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return false;
    }

    public string ShortName => Symbol.DisplayName;
    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;
  }
}