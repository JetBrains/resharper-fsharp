using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedMemberBase : FSharpGeneratedElementBase, IFSharpTypeMember
  {
    protected PredefinedType PredefinedType =>
      ContainingElement.Module.GetPredefinedType();

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

    public bool IsExplicitImplementation => false;
    public bool CanBeImplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;

    public Hash? CalcHash() => null;
    public ITypeElement ContainingType => GetContainingType();

    public AccessibilityDomain AccessibilityDomain =>
      new AccessibilityDomain(AccessibilityDomain.AccessibilityDomainType.PUBLIC, null);

    public virtual MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_NAME;

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      // We should check generated member type instead
      // but we don't handle source member overrides like ToString() correctly yet.
      if (!(obj is IFSharpTypeMember other))
        return false;

      if (!ShortName.Equals(other.ShortName))
        return false;

      return Equals(GetContainingType(), other.GetContainingType());
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public bool IsExtensionMember => false;

    public FSharpSymbol Symbol => null;

    public IList<ITypeParameter> AllTypeParameters =>
      GetContainingType().GetAllTypeParameters().ResultingList().Reverse();
  }
}
