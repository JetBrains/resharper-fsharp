using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpMemberBase<TDeclaration> : FSharpTypeMember<TDeclaration>, IParametersOwner,
    IOverridableMember, IFSharpExtensionTypeMember
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpMemberBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration)
    {
    }

    [CanBeNull] public FSharpMemberOrFunctionOrValue Mfv => Symbol as FSharpMemberOrFunctionOrValue;

    public override bool IsExtensionMember => Mfv?.IsExtensionMember ?? false;
    public override bool IsFSharpMember => Mfv?.IsMember ?? false;

    protected override ITypeElement GetTypeElement(IDeclaration declaration)
    {
      var typeDeclaration = declaration.GetContainingNode<ITypeDeclaration>();
      if (typeDeclaration is ITypeExtensionDeclaration extension && !extension.IsTypePartDeclaration)
        return extension.GetContainingNode<ITypeDeclaration>()?.DeclaredElement;

      return typeDeclaration?.DeclaredElement;
    }

    protected IList<FSharpAttribute> Attributes =>
      Mfv?.Attributes ?? EmptyList<FSharpAttribute>.Instance;

    public override IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      Attributes.ToAttributeInstances(Module);

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      Attributes.GetAttributes(clrName).ToAttributeInstances(Module);

    public override bool HasAttributeInstance(IClrTypeName clrName, bool inherit) =>
      Attributes.HasAttributeInstance(clrName.FullName);

    public InvocableSignature GetSignature(ISubstitution substitution) => new InvocableSignature(this, substitution);

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      EmptyList<IParametersOwnerDeclaration>.Instance;

    public virtual IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public virtual ReferenceKind ReturnKind => ReferenceKind.VALUE;
    public abstract IType ReturnType { get; }

    public override AccessRights GetAccessRights()
    {
      var mfv = Mfv;
      if (mfv == null)
        return AccessRights.NONE;

      var accessibility = mfv.Accessibility;
      if (accessibility.IsInternal)
        return AccessRights.INTERNAL;
      if (accessibility.IsPrivate)
        return AccessRights.PRIVATE;
      return AccessRights.PUBLIC;
    }

    public override bool IsStatic => !Mfv?.IsInstanceMember ?? false;

    // todo: check interface impl
    public override bool IsOverride => Mfv?.IsOverrideOrExplicitInterfaceImplementation ?? false;
    public override bool IsAbstract => Mfv?.IsDispatchSlot ?? false;
    public override bool IsVirtual => false; // todo

    public override bool Equals(object obj)
    {
      if (!base.Equals(obj) || !(obj is FSharpMemberBase<TDeclaration> otherMember))
        return false;

      var mfv = Mfv;
      if (mfv == null)
        return false;

      var isExtension = mfv.IsExtensionMember;
      var isInstanceMember = mfv.IsInstanceMember;

      if (!(isExtension && isInstanceMember))
        return true;

      var otherSymbol = otherMember.Mfv;
      if (otherSymbol == null)
        return false;

      if (!otherSymbol.IsExtensionMember || !otherSymbol.IsInstanceMember)
        return false;

      var apparentEntity = mfv.ApparentEnclosingEntity;
      var otherApparentEntity = otherSymbol.ApparentEnclosingEntity;
      return apparentEntity.Equals(otherApparentEntity);
    }

    public override int GetHashCode() => ShortName.GetHashCode();
    public FSharpEntity ApparentEntity => Mfv?.ApparentEnclosingEntity;
  }
}
