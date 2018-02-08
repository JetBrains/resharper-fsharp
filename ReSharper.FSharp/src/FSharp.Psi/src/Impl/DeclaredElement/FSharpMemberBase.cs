using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpMemberBase<TDeclaration> : FSharpTypeMember<TDeclaration>, IParametersOwner,
    IOverridableMember
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    [NotNull]
    public FSharpMemberOrFunctionOrValue FSharpSymbol { get; }

    protected FSharpMemberBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration)
    {
      FSharpSymbol = mfv;
    }


    public override IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      FSharpAttributeInstance.GetAttributeInstances(FSharpSymbol.Attributes, Module);

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      FSharpAttributeInstance.GetAttributeInstances(FSharpSymbol.Attributes.GetAttributes(clrName), Module);

    public override bool HasAttributeInstance(IClrTypeName clrName, bool inherit) =>
      FSharpSymbol.Attributes.HasAttributeInstance(clrName.FullName);

    public InvocableSignature GetSignature(ISubstitution substitution) => new InvocableSignature(this, substitution);

    public override bool Equals(object obj)
    {
      if (!base.Equals(obj))
        return false;

      if (!(obj is FSharpMemberBase<TDeclaration> member) || IsStatic != member.IsStatic) // RIDER-11321, RSRP-467025
        return false;

      return SignatureComparers.Strict.Compare(GetSignature(IdSubstitution),
        member.GetSignature(member.IdSubstitution));
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      EmptyList<IParametersOwnerDeclaration>.Instance;

    public virtual IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public ReferenceKind ReturnKind => ReferenceKind.VALUE;
    public abstract override string ShortName { get; }
    public abstract IType ReturnType { get; }

    public override AccessRights GetAccessRights()
    {
      var accessibility = FSharpSymbol.Accessibility;
      if (accessibility.IsInternal)
        return AccessRights.INTERNAL;
      if (accessibility.IsPrivate)
        return AccessRights.PRIVATE;
      return AccessRights.PUBLIC;
    }

    public override bool IsStatic => !FSharpSymbol.IsInstanceMember;

    // todo: check interface impl
    public override bool IsOverride => FSharpSymbol.IsOverrideOrExplicitInterfaceImplementation;

    public override bool IsAbstract => FSharpSymbol.IsDispatchSlot;
    public override bool IsVirtual => false; // todo
    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;
    public bool CanBeImplicitImplementation => true; // todo: set false and calc proper base element
  }
}