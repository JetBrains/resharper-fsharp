using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpDeclaredElement<TDeclaration> : CachedTypeMemberBase
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration
  {
    protected FSharpDeclaredElement([NotNull] IDeclaration declaration) : base(declaration)
    {
    }

    protected override bool CanBindTo(IDeclaration declaration)
    {
      return declaration is TDeclaration;
    }

    [CanBeNull]
    public new TDeclaration GetDeclaration()
    {
      return (TDeclaration) base.GetDeclaration();
    }

    public bool IsSynthetic()
    {
      return false;
    }

    public bool CaseSensitiveName => true;

    public virtual string SourceName =>
      GetDeclaration()?.SourceName ??
      SharedImplUtil.MISSING_DECLARATION_NAME;
    
    public virtual string ShortName =>
      GetDeclaration()?.DeclaredName ??
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public virtual ISubstitution IdSubstitution =>
      GetContainingType()?.IdSubstitution ??
      EmptySubstitution.INSTANCE;

    // ReSharper disable once InconsistentNaming
    public XmlNode GetXMLDoc(bool inherit)
    {
      return null; // todo
    }

    // ReSharper disable once InconsistentNaming
    public XmlNode GetXMLDescriptionSummary(bool inherit)
    {
      return null; // todo
    }

    public abstract DeclaredElementType GetElementType();
  }
}