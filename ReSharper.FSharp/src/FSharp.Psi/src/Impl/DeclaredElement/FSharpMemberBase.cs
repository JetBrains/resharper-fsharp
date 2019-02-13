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
    IOverridableMember, IFSharpMember
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

    public override bool IsExtensionMember => FSharpSymbol.IsExtensionMember;
    public override bool IsMember => FSharpSymbol.IsMember;

    protected override ITypeElement GetTypeElement(IDeclaration declaration)
    {
      var typeDeclaration = declaration.GetContainingNode<ITypeDeclaration>();
      if (typeDeclaration is ITypeExtensionDeclaration extensionDeclaration &&
          !extensionDeclaration.IsTypePartDeclaration)
        return extensionDeclaration.GetContainingNode<ITypeDeclaration>()?.DeclaredElement;

      return typeDeclaration?.DeclaredElement;
    }

    public override IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      FSharpAttributeInstance.GetAttributeInstances(FSharpSymbol.Attributes, Module);

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      FSharpAttributeInstance.GetAttributeInstances(FSharpSymbol.Attributes.GetAttributes(clrName), Module);

    public override bool HasAttributeInstance(IClrTypeName clrName, bool inherit) =>
      FSharpSymbol.Attributes.HasAttributeInstance(clrName.FullName);

    public InvocableSignature GetSignature(ISubstitution substitution) => new InvocableSignature(this, substitution);

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      EmptyList<IParametersOwnerDeclaration>.Instance;

    public virtual IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public virtual ReferenceKind ReturnKind => ReferenceKind.VALUE;
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

    public override bool Equals(object obj)
    {
      if (!base.Equals(obj) || !(obj is FSharpMemberBase<TDeclaration> otherMember))
        return false;

      var isExtension = FSharpSymbol.IsExtensionMember;
      var isInstanceMember = FSharpSymbol.IsInstanceMember;

      if (!(isExtension && isInstanceMember))
        return true;

      var otherSymbol = otherMember.FSharpSymbol;
      if (!otherSymbol.IsExtensionMember || !otherSymbol.IsInstanceMember)
        return false;

      var apparentEntity = FSharpSymbol.ApparentEnclosingEntity;
      var otherApparentEntity = otherSymbol.ApparentEnclosingEntity;
      return apparentEntity.Equals(otherApparentEntity);
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public FSharpEntity ApparentDeclaringEntity => FSharpSymbol.ApparentEnclosingEntity;

    public override bool IsStatic => !FSharpSymbol.IsInstanceMember;

    // todo: check interface impl
    public override bool IsOverride => FSharpSymbol.IsOverrideOrExplicitInterfaceImplementation;

    public override bool IsAbstract => FSharpSymbol.IsDispatchSlot;
    public override bool IsVirtual => false; // todo
    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;
    public bool CanBeImplicitImplementation => true; // todo: set false and calc proper base element
  }

  public interface IFSharpMember
  {
    FSharpEntity ApparentDeclaringEntity { get; }
  }
}
