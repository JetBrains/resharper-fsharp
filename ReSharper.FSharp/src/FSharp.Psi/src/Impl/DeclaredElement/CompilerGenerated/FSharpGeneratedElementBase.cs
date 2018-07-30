using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedElementBase : IClrDeclaredElement
  {
    [NotNull]
    protected abstract IClrDeclaredElement ContainingElement { get; } 

    public abstract string ShortName { get; }
    public abstract DeclaredElementType GetElementType();
    public abstract ITypeElement GetContainingType();
    public abstract ITypeMember GetContainingTypeMember();

    public virtual bool IsValid() => ContainingElement.IsValid();
    public IPsiServices GetPsiServices() => ContainingElement.GetPsiServices();

    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;

    public IPsiModule Module => ContainingElement.Module;
    public virtual ISubstitution IdSubstitution => ContainingElement.IdSubstitution;

    public IList<IDeclaration> GetDeclarations() =>
      EmptyList<IDeclaration>.Instance;

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) =>
      EmptyList<IDeclaration>.Instance;

    public HybridCollection<IPsiSourceFile> GetSourceFiles() =>
      HybridCollection<IPsiSourceFile>.Empty;

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile) => false;

    public XmlNode GetXMLDoc(bool inherit) => null;
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null;    

    public bool IsSynthetic() => false;
    public virtual bool IsVisibleFromFSharp => false;
  }
}
