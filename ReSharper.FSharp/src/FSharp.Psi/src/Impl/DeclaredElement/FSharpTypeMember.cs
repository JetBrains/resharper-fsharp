using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpTypeMember<TDeclaration> : FSharpDeclaredElement<TDeclaration>, IFSharpTypeMember
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpTypeMember([NotNull] IDeclaration declaration) : base(declaration)
    {
    }

    public ITypeMember GetContainingTypeMember()
    {
      return (ITypeMember) GetContainingType();
    }

    public override IList<IDeclaration> GetDeclarations()
    {
      return GetPartialDeclarations(null);
    }

    public override IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return GetPartialDeclarations(sourceFile);
    }

    private IList<IDeclaration> GetPartialDeclarations([CanBeNull] IPsiSourceFile sourceFile)
    {
      var containingType = GetContainingType();
      if (containingType == null)
        return EmptyList<IDeclaration>.InstanceList;

      var declaration = GetDeclaration();
      if (declaration == null)
        return EmptyList<IDeclaration>.InstanceList;

      var list = new FrugalLocalList<IDeclaration>();
      var declarations =
        sourceFile != null
          ? containingType.GetDeclarationsIn(sourceFile)
          : containingType.GetDeclarations();

      foreach (var partDeclaration in declarations)
      {
        var typeDeclaration = partDeclaration as IFSharpTypeElementDeclaration;
        if (typeDeclaration == null) continue;

        foreach (var member in typeDeclaration.MemberDeclarations)
          if (member.DeclaredName == declaration.DeclaredName && Equals(this, member.DeclaredElement))
            list.Add(member);
      }
      return list.AsIList();
    }

    public override HybridCollection<IPsiSourceFile> GetSourceFiles()
    {
      return GetContainingType()?.GetSourceFiles() ??
             HybridCollection<IPsiSourceFile>.Empty;
    }

    public override bool HasDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return GetSourceFiles().Contains(sourceFile);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!(obj is FSharpTypeMember<TDeclaration> member)) return false;

      if (!ShortName.Equals(member.ShortName))
        return false;

      return Equals(GetContainingType(), member.GetContainingType());
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public virtual IList<IAttributeInstance> GetAttributeInstances(bool inherit)
    {
      return EmptyList<IAttributeInstance>.Instance;
    }

    public virtual IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit)
    {
      return EmptyList<IAttributeInstance>.Instance;
    }

    public virtual bool HasAttributeInstance(IClrTypeName clrName, bool inherit)
    {
      return false;
    }

    public virtual AccessRights GetAccessRights()
    {
      return GetDeclaration()?.GetAccessRights() ?? AccessRights.PUBLIC;
    }

    // todo
    public virtual bool IsAbstract => false;

    public virtual bool IsSealed => false;
    public virtual bool IsVirtual => false;
    public virtual bool IsOverride => false;
    public virtual bool IsStatic => false;
    public virtual bool IsReadonly => false;
    public virtual bool IsExtern => false;
    public virtual bool IsUnsafe => false;
    public virtual bool IsVolatile => false;
    public string XMLDocId => XMLDocUtil.GetTypeMemberXmlDocId(this, ShortName);

    public IList<TypeMemberInstance> GetHiddenMembers() => HiddenMemberImpl.GetHiddenMembers(this);

    public AccessibilityDomain AccessibilityDomain =>
      new AccessibilityDomain(AccessibilityDomain.AccessibilityDomainType.PUBLIC, null);

    public MemberHidePolicy HidePolicy => this is IParametersOwner
      ? MemberHidePolicy.HIDE_BY_SIGNATURE
      : MemberHidePolicy.HIDE_BY_NAME;

    public virtual bool IsVisibleFromFSharp => true;
    public virtual bool IsExtensionMember => false;
    public abstract bool IsMember { get; }
  }
}