using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
{
  public class FSharpFakeElementFromReference : IClrDeclaredElement
  {
    [NotNull] public readonly FSharpSymbol Symbol;
    [NotNull] private readonly FSharpIdentifierToken myReferenceOwner;

    public FSharpFakeElementFromReference([NotNull] FSharpSymbol symbol, [NotNull] FSharpIdentifierToken referenceOwner)
    {
      Symbol = symbol;
      myReferenceOwner = referenceOwner;
    }

    [CanBeNull]
    public IClrDeclaredElement GetActualElement()
    {
      return FSharpElementsUtil.GetDeclaredElement(Symbol, myReferenceOwner.GetPsiModule());
    }

    public IPsiServices GetPsiServices()
    {
      return myReferenceOwner.GetPsiServices();
    }

    public IList<IDeclaration> GetDeclarations()
    {
      // todo: after caches are ready null actual element will mean local-declared element, so should return this file
      return GetActualElement()?.GetDeclarations() ?? EmptyList<IDeclaration>.Instance;
    }

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
    {
      // todo: after caches are ready null actual element will mean local-declared element, so should return this file
      return GetActualElement()?.GetDeclarationsIn(sourceFile) ?? EmptyList<IDeclaration>.Instance;
    }

    public DeclaredElementType GetElementType()
    {
      return GetActualElement()?.GetElementType() ?? CLRDeclaredElementType.LOCAL_VARIABLE;
    }

    public XmlNode GetXMLDoc(bool inherit)
    {
      return GetActualElement()?.GetXMLDoc(inherit);
    }

    public XmlNode GetXMLDescriptionSummary(bool inherit)
    {
      return GetActualElement()?.GetXMLDescriptionSummary(inherit);
    }

    public bool IsValid()
    {
      return myReferenceOwner.IsValid();
    }

    public bool IsSynthetic()
    {
      return false;
    }

    public HybridCollection<IPsiSourceFile> GetSourceFiles()
    {
      return GetActualElement()?.GetSourceFiles() ?? myReferenceOwner.GetSourceFiles();
    }

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return GetActualElement()?.HasDeclarationsIn(sourceFile) ?? myReferenceOwner.GetSourceFile() == sourceFile;
    }

    public string ShortName => myReferenceOwner.GetText();
    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;

    public ITypeElement GetContainingType()
    {
      return GetActualElement()?.GetContainingType();
    }

    public ITypeMember GetContainingTypeMember()
    {
      return GetActualElement()?.GetContainingTypeMember();
    }

    public IPsiModule Module => GetActualElement()?.Module ?? myReferenceOwner.GetPsiModule();
    public ISubstitution IdSubstitution => GetActualElement()?.IdSubstitution ?? EmptySubstitution.INSTANCE;
  }
}