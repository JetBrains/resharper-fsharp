using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedMemberBase : FSharpGeneratedElementBase, IFSharpTypeMember
  {
    protected PredefinedType PredefinedType =>
      ContainingElement.Module.GetPredefinedType();

    public IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      EmptyList<IAttributeInstance>.Instance;

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      EmptyList<IAttributeInstance>.Instance;

    public bool HasAttributeInstance(IClrTypeName clrName, bool inherit) => false;

    public virtual AccessRights GetAccessRights() => AccessRights.PUBLIC;

    public virtual bool IsAbstract => false;
    public virtual bool IsSealed => false;
    public virtual bool IsVirtual => false;
    public virtual bool IsOverride => false;
    public virtual bool IsStatic => false;
    public virtual bool IsReadonly => false;
    public virtual bool IsExtern => false;
    public virtual bool IsUnsafe => false;
    public virtual bool IsVolatile => false;

    public virtual string XMLDocId =>
      XMLDocUtil.GetTypeMemberXmlDocId(this, ShortName);

    public IList<TypeMemberInstance> GetHiddenMembers() =>
      EmptyList<TypeMemberInstance>.Instance;

    public Hash? CalcHash() => null;

    public AccessibilityDomain AccessibilityDomain =>
      new AccessibilityDomain(AccessibilityDomain.AccessibilityDomainType.PUBLIC, null);

    public virtual MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_NAME;

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!(obj is IFSharpTypeMember member))
        return false;

      if (!ShortName.Equals(member.ShortName))
        return false;

      return Equals(GetContainingType(), member.GetContainingType());
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    
    public bool IsExtensionMember => false;
    public bool IsMember => true;
  }
}
