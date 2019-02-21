using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpCachedTypeMemberBase<TDeclaration> : CachedTypeMemberBase
    where TDeclaration : IFSharpDeclaration
  {
    protected FSharpCachedTypeMemberBase([NotNull] IDeclaration declaration) : base(declaration)
    {
    }

    protected override bool CanBindTo(IDeclaration declaration) => declaration is TDeclaration;

    [CanBeNull]
    public new TDeclaration GetDeclaration() => (TDeclaration) base.GetDeclaration();

    public bool IsSynthetic() => false;
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
    public XmlNode GetXMLDoc(bool inherit) => null; // todo

    // ReSharper disable once InconsistentNaming
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null; // todo

    public abstract DeclaredElementType GetElementType();

    [CanBeNull] protected TypeElement ContainingType => GetContainingType() as TypeElement;
  }
}
