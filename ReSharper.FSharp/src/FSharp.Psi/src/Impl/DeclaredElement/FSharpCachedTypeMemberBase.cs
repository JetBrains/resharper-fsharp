using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

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
      GetDeclaration()?.CompiledName ??
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public virtual ISubstitution IdSubstitution =>
      GetContainingType()?.IdSubstitution ??
      EmptySubstitution.INSTANCE;

    // ReSharper disable once InconsistentNaming
    public virtual XmlNode GetXMLDoc(bool inherit) =>
      GetDeclaration() is { XmlDocBlock: { } xmlDocBlock } ? xmlDocBlock.GetXML(this as ITypeMember) : null;

    // ReSharper disable once InconsistentNaming
    public XmlNode GetXMLDescriptionSummary(bool inherit) =>
      XMLDocUtil.ExtractSummary(GetXMLDoc(inherit));

    public abstract DeclaredElementType GetElementType();
  }
}
