using System.Collections.Generic;
using System.Xml;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public abstract class DeclaredElementBase : IClrDeclaredElement
  {
    public bool CaseSensitiveName => true;
    public bool IsSynthetic() => false;

    public IList<IDeclaration> GetDeclarations() => EmptyList<IDeclaration>.Instance;
    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) => EmptyList<IDeclaration>.Instance;

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile) => false;
    public HybridCollection<IPsiSourceFile> GetSourceFiles() => HybridCollection<IPsiSourceFile>.Empty;

    public virtual bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) => false;
    public virtual IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) => EmptyList<IAttributeInstance>.Instance;

    public virtual IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource attributesSource) =>
      EmptyList<IAttributeInstance>.Instance;

    public virtual XmlNode GetXMLDoc(bool inherit) => null;
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null;

    public abstract string ShortName { get; }
    public abstract bool IsValid();

    public abstract PsiLanguageType PresentationLanguage { get; }
    public abstract IPsiModule Module { get; }
    public abstract IPsiServices GetPsiServices();

    public abstract ITypeElement GetContainingType();
    public abstract ITypeMember GetContainingTypeMember();

    public abstract ISubstitution IdSubstitution { get; }
    public abstract DeclaredElementType GetElementType();
  }

  public abstract class FSharpDeclaredElementBase : DeclaredElementBase
  {
    public override PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;
  }
}
