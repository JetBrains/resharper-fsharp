using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedElementBase : IClrDeclaredElement
  {
    [NotNull] private readonly IClass myContainingType;

    protected FSharpGeneratedElementBase([NotNull] IClass containingType)
    {
      myContainingType = containingType;
    }

    public IPsiServices GetPsiServices()
    {
      return myContainingType.GetPsiServices();
    }

    public IList<IDeclaration> GetDeclarations()
    {
      return EmptyList<IDeclaration>.Instance;
    }

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return EmptyList<IDeclaration>.Instance;
    }

    public abstract DeclaredElementType GetElementType();

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
      return myContainingType.IsValid();
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

    public abstract string ShortName { get; }
    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;

    public virtual ITypeElement GetContainingType()
    {
      return myContainingType;
    }

    public virtual ITypeMember GetContainingTypeMember()
    {
      return myContainingType;
    }

    public IPsiModule Module => myContainingType.Module;
    public virtual ISubstitution IdSubstitution => myContainingType.IdSubstitution;
  }
}