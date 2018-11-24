using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedMemberBase : FSharpGeneratedElementBase, IFSharpTypeMember
  {
    protected FSharpGeneratedMemberBase([NotNull] IClass containingType) : base(containingType)
    {
    }

    public abstract override DeclaredElementType GetElementType();
    public abstract override string ShortName { get; }

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

    public AccessRights GetAccessRights()
    {
      return AccessRights.PUBLIC;
    }

    public ReferenceKind ReturnKind => ReferenceKind.VALUE;

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

    public Hash? CalcHash()
    {
      return null;
    }

    public AccessibilityDomain AccessibilityDomain => new AccessibilityDomain(
      AccessibilityDomain.AccessibilityDomainType.PUBLIC, null);

    public abstract MemberHidePolicy HidePolicy { get; }
    public virtual bool IsVisibleFromFSharp => false;
    public bool IsExtensionMember => false;
    public bool IsMember => true;
  }
}