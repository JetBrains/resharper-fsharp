using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal abstract class FSharpMemberBase<TDeclaration> : FSharpTypeMember<TDeclaration>, IParametersOwner
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    private readonly AccessRights myAccessRights;

    protected FSharpMemberBase([NotNull] ITypeMemberDeclaration declaration,
      [CanBeNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration)
    {
      if (mfv == null)
      {
        IsStatic = false;
        IsOverride = false;
        IsVirtual = false;
        IsAbstract = false;
      }
      else
      {
        if (mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod)
        {
        }

        var members = mfv.EnclosingEntity.MembersFunctionsAndValues;
        var hasAbstract = members.Any(m => m.LogicalName == mfv.LogicalName && m.IsDispatchSlot);
        var hasDefault = members.Any(m => m.LogicalName == mfv.LogicalName &&
                                          m.IsOverrideOrExplicitInterfaceImplementation);
        var isOverride = mfv.IsOverrideOrExplicitInterfaceImplementation;
        var isAbstract = mfv.IsDispatchSlot;

        IsStatic = !mfv.IsInstanceMember;
        IsOverride = isOverride;
        IsVirtual = hasAbstract && hasDefault;
        IsAbstract = isAbstract && !hasDefault;
      }


      var accessibility = mfv?.Accessibility;
      if (accessibility == null)
        myAccessRights = AccessRights.PUBLIC;
      else
      {
        if (accessibility.IsPrivate)
          myAccessRights = AccessRights.PRIVATE;
        else if (accessibility.IsInternal)
          myAccessRights = AccessRights.INTERNAL;
        else
          myAccessRights = AccessRights.PUBLIC;
      }
    }

    public InvocableSignature GetSignature(ISubstitution substitution)
    {
      return new InvocableSignature(this, substitution);
    }

    public override bool Equals(object obj)
    {
      if (!base.Equals(obj))
        return false;

      var member = obj as FSharpMemberBase<TDeclaration>;

      return member != null &&
             SignatureComparers.Strict.Compare(GetSignature(IdSubstitution),
               member.GetSignature(member.IdSubstitution));
    }

    public override int GetHashCode()
    {
      return ShortName.GetHashCode();
    }

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations()
    {
      return EmptyList<IParametersOwnerDeclaration>.Instance;
    }

    public virtual IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public bool IsRefReturn => false;
    public abstract override string ShortName { get; }
    public abstract IType ReturnType { get; }

    public override AccessRights GetAccessRights()
    {
      return myAccessRights;
    }

    public override bool IsStatic { get; }
    public override bool IsOverride { get; }
    public override bool IsAbstract { get; }
    public override bool IsVirtual { get; }
    public bool CanBeImplicitImplementation => true;
  }
}