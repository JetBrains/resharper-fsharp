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
    [NotNull] private readonly FSharpSymbol mySymbol;
    [NotNull] private readonly FSharpIdentifierToken myReferenceOwner;

    public FSharpFakeElementFromReference([NotNull] FSharpSymbol symbol, [NotNull] FSharpIdentifierToken referenceOwner)
    {
      mySymbol = symbol;
      myReferenceOwner = referenceOwner;
    }

    [CanBeNull]
    private IClrDeclaredElement GetActualElement()
    {
      return FSharpElementsUtil.GetDeclaredElement(mySymbol, myReferenceOwner.GetPsiModule());
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
      throw new System.NotImplementedException(); // todo
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
      return GetActualElement()?.IsValid() ?? myReferenceOwner.IsValid();
    }

    public bool IsSynthetic()
    {
      return GetActualElement()?.IsSynthetic() ?? false;
    }

    public HybridCollection<IPsiSourceFile> GetSourceFiles()
    {
      return GetActualElement()?.GetSourceFiles() ?? myReferenceOwner.GetSourceFiles();
    }

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return GetActualElement()?.HasDeclarationsIn(sourceFile) ?? myReferenceOwner.GetSourceFile() == sourceFile;
    }

    public string ShortName => GetActualElement()?.ShortName ?? myReferenceOwner.GetText();
    public bool CaseSensitiveName => GetActualElement()?.CaseSensitiveName ?? true;
    public PsiLanguageType PresentationLanguage => GetActualElement()?.PresentationLanguage ?? FSharpLanguage.Instance;

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