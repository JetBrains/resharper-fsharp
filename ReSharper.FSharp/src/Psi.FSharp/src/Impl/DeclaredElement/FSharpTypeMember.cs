using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
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


    public IList<IAttributeInstance> GetAttributeInstances(bool inherit)
    {
      return EmptyList<IAttributeInstance>.Instance;
    }

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit)
    {
      return EmptyList<IAttributeInstance>.Instance;
    }

    public bool HasAttributeInstance(IClrTypeName clrName, bool inherit)
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
    public string XMLDocId => ShortName;

    public IList<TypeMemberInstance> GetHiddenMembers()
    {
      return EmptyList<TypeMemberInstance>.Instance;
    }

    public AccessibilityDomain AccessibilityDomain =>
      new AccessibilityDomain(AccessibilityDomain.AccessibilityDomainType.PUBLIC, null);

    public MemberHidePolicy HidePolicy => this is IParametersOwner
      ? MemberHidePolicy.HIDE_BY_SIGNATURE
      : MemberHidePolicy.HIDE_BY_NAME;

    public virtual bool IsVisibleFromFSharp => true;
  }
}