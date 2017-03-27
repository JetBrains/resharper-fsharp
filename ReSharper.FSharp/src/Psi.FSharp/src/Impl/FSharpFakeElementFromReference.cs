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

    /// <summary>
    /// Should be used on local declarations only
    /// </summary>
    [CanBeNull]
    public ITreeNode GetContainingTypeMemberDeclaration()
    {
      var mfv = Symbol as FSharpMemberOrFunctionOrValue;
      Assertion.Assert(mfv != null && !mfv.IsModuleValueOrMember,
        $"Getting local declaratiion for top value: {Symbol}");
      return myReferenceOwner.GetContainingNode<ITypeMemberDeclaration>();
    }

    [CanBeNull]
    public IClrDeclaredElement GetActualElement()
    {
      var mfv = Symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null && !mfv.IsModuleValueOrMember)
        return FindLocalDeclaration(mfv);

      return FSharpElementsUtil.GetDeclaredElement(Symbol, myReferenceOwner.GetPsiModule());
    }

    private IClrDeclaredElement FindLocalDeclaration([NotNull] FSharpMemberOrFunctionOrValue mfv)
    {
      var declRange = Symbol.DeclarationLocation;
      if (declRange == null)
        return null;

      var document = myReferenceOwner.GetSourceFile()?.Document;
      if (document == null)
        return null;

      var fsFile = myReferenceOwner.GetContainingFile();
      Assertion.AssertNotNull(fsFile, "fsFile != null");
      var idToken = fsFile.FindTokenAt(FSharpRangeUtil.GetEndOffset(document, declRange.Value) - 1);
      return idToken?.GetContainingNode<LocalDeclaration>();
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
      return null;
    }

    public ITypeMember GetContainingTypeMember()
    {
      return null;
    }

    public IPsiModule Module => myReferenceOwner.GetPsiModule();
    public ISubstitution IdSubstitution => GetActualElement()?.IdSubstitution ?? EmptySubstitution.INSTANCE;
  }
}