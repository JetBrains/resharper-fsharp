using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal abstract class FSharpMemberBase<TDeclaration> : FSharpTypeMember<TDeclaration>, IParametersOwner
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    [CanBeNull]
    public FSharpMemberOrFunctionOrValue Mfv { get; }

    private readonly AccessRights myAccessRights;

    protected FSharpMemberBase([NotNull] ITypeMemberDeclaration declaration,
      [CanBeNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration)
    {
      Mfv = mfv;
      if (mfv == null || !mfv.IsMember)
      {
        IsOverride = false;
        IsVirtual = false;
        IsAbstract = false;
      }
      else
      {
        // todo: overloads
        var entity = mfv.EnclosingEntity;
        if (entity.QualifiedName.SubstringBefore(",") != declaration.GetContainingTypeDeclaration()?.CLRName)
        {
          IsOverride = true;
          IsAbstract = false;
          IsVirtual = false;
        }
        else
        {
          var members = entity.MembersFunctionsAndValues.Where(m => m.LogicalName == mfv.LogicalName).AsList();
          var hasAbstract = members.Any(m => m.IsDispatchSlot);
          var hasDefault = members.Any(m => m.IsOverrideOrExplicitInterfaceImplementation);

          IsOverride = mfv.IsOverrideOrExplicitInterfaceImplementation;
          IsVirtual = hasAbstract && hasDefault;
          IsAbstract = mfv.IsDispatchSlot && !hasDefault;
        }
      }

      IsStatic = !mfv?.IsInstanceMember ?? false;

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


    public override IList<IAttributeInstance> GetAttributeInstances(bool inherit)
    {
      if (Mfv == null)
        return EmptyList<IAttributeInstance>.Instance;

      var attrs = new List<IAttributeInstance>();
      foreach (var attr in Mfv.Attributes)
        attrs.Add(new FSharpAttributeInstance(attr, Module));
      return attrs;
    }

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit)
    {
      if (Mfv == null)
        return EmptyList<IAttributeInstance>.Instance;

      var attrs = new List<IAttributeInstance>();
      foreach (var attr in Mfv.Attributes)
        if (attr.AttributeType.FullName == clrName.FullName)
          attrs.Add(new FSharpAttributeInstance(attr, Module));
      return attrs;
    }

    public override bool HasAttributeInstance(IClrTypeName clrName, bool inherit)
    {
      if (Mfv == null)
        return false;

      return Mfv.Attributes.Any(a => a.AttributeType.FullName == clrName.FullName);
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