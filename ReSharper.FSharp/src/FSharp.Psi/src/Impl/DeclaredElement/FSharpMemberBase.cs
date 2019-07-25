using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpMemberBase<TDeclaration> : FSharpTypeMember<TDeclaration>,
    IOverridableMember, IFSharpMember
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpMemberBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public FSharpMemberOrFunctionOrValue Mfv => Symbol as FSharpMemberOrFunctionOrValue;

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

    public override IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      Attributes.ToAttributeInstances(Module);

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource attributesSource) =>
      Attributes.GetAttributes(clrName).ToAttributeInstances(Module);

    public override bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) =>
      Attributes.HasAttributeInstance(clrName.FullName);

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      EmptyList<IParametersOwnerDeclaration>.Instance;

    public virtual IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public virtual ReferenceKind ReturnKind => ReferenceKind.VALUE;
    public abstract IType ReturnType { get; }

    public override AccessRights GetAccessRights()
    {
      if (IsExplicitImplementation)
        return AccessRights.PRIVATE;

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

    public bool CanBeImplicitImplementation => false;

    public bool IsExplicitImplementation =>
      GetDeclaration() is IMemberDeclaration member && member.IsExplicitImplementation;

    public IList<IExplicitImplementation> ExplicitImplementations
    {
      get
      {
        var mfv = Mfv;
        var implementations = mfv?.ImplementedAbstractSignatures;
        if (implementations == null || implementations.IsEmpty())
          return EmptyList<IExplicitImplementation>.Instance;

        if (implementations.Count > 1)
          Logger.GetLogger<FSharpMemberBase<TDeclaration>>().Warn("Multiple explicit implementations for {0}", this);

        var impl = implementations.FirstOrDefault();
        if (impl == null)
          return EmptyList<IExplicitImplementation>.Instance;

        return GetType(impl.DeclaringType) is IDeclaredType type
          ? new IExplicitImplementation[] {new ExplicitImplementation(this, type, ShortName, true)}
          : EmptyList<IExplicitImplementation>.InstanceList;
      }
    }

    // todo: check interface impl
    public override bool IsOverride => Mfv?.IsOverrideOrExplicitInterfaceImplementation ?? false;
    public override bool IsAbstract => Mfv?.IsDispatchSlot ?? false;
    public override bool IsVirtual => false; // todo

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!base.Equals(obj) || !(obj is FSharpMemberBase<TDeclaration> otherMember))
        return false;

      if (IsExplicitImplementation != otherMember.IsExplicitImplementation)
        return false;

      var mfv = Mfv;
      if (mfv == null)
        return false;

      if (!mfv.IsExtensionMember)
        return true;

      var otherSymbol = otherMember.Mfv;
      if (otherSymbol == null)
        return false;

      if (!otherSymbol.IsExtensionMember)
        return false;

      var apparentEntity = mfv.ApparentEnclosingEntity;
      var otherApparentEntity = otherSymbol.ApparentEnclosingEntity;
      return apparentEntity.Equals(otherApparentEntity);
    }

    public override int GetHashCode() => ShortName.GetHashCode();
  }
}
