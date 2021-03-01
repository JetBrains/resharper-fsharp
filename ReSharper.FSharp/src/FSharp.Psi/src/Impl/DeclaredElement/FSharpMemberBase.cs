using System.Collections.Generic;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpMemberBase<TDeclaration> : FSharpTypeMember<TDeclaration>, IFSharpMember
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpMemberBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public FSharpMemberOrFunctionOrValue Mfv => Symbol as FSharpMemberOrFunctionOrValue;

    public bool IsExtensionMember =>
      GetContainingType() is IFSharpModule && GetDeclaration() is IMemberSignatureOrDeclaration;

    protected override ITypeElement GetTypeElement(IDeclaration declaration)
    {
      var typeDeclaration = declaration.GetContainingNode<ITypeDeclaration>();
      if (typeDeclaration is ITypeExtensionDeclaration { IsTypePartDeclaration: false } extension)
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

      // Workaround to hide extension methods from resolve in C#.
      // todo: calc compiled names for extension members (it'll hide needed ones properly)
      // todo: implement F# declared element presenter to hide compiled names in features/ui
      if (IsExtensionMember && GetDeclaration() is IMemberSignatureOrDeclaration memberDeclaration)
        if (!(this is IMethod && memberDeclaration.Attributes.GetCompiledName(out _)))
          return AccessRights.INTERNAL;

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
      GetDeclaration() is IMemberDeclaration { IsExplicitImplementation: true };

    public IList<IExplicitImplementation> ExplicitImplementations
    {
      get
      {
        var mfv = Mfv;
        if (mfv == null)
          return EmptyList<IExplicitImplementation>.Instance;

        if (GetDeclaration() is IMemberDeclaration member && ObjExprNavigator.GetByMember(member) != null)
          return mfv.DeclaringEntity?.Value is { } entity && entity.GetTypeElement(Module) is { } typeElement
            ? new IExplicitImplementation[]
              {new ExplicitImplementation(this, TypeFactory.CreateType(typeElement), ShortName, true)}
            : EmptyList<IExplicitImplementation>.InstanceList;

        var implementations = mfv.ImplementedAbstractSignatures;
        if (implementations.IsNullOrEmpty())
          return EmptyList<IExplicitImplementation>.Instance;

        var result = new LocalList<IExplicitImplementation>();
        foreach (var impl in implementations)
          if (GetType(impl.DeclaringType) is IDeclaredType type)
            result.Add(new ExplicitImplementation(this, type, ShortName, true));

        return result.ResultingList();
      }
    }

    public override bool IsOverride =>
      GetDeclaration() is { } decl &&
      (decl.IsOverride || InterfaceImplementationNavigator.GetByTypeMember(decl as IMemberDeclaration) != null);

    public override bool IsAbstract =>
      GetDeclaration() is IAbstractMemberDeclaration { HasDefaultImplementation: false };

    public override bool IsVirtual =>
      GetDeclaration() switch
      {
        IMemberSignatureOrDeclaration memberDeclaration => memberDeclaration.IsVirtual,
        IAbstractMemberDeclaration memberDeclaration => memberDeclaration.HasDefaultImplementation,
        _ => false
      };

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!(obj is IFSharpMember otherMember) || !base.Equals(obj))
        return false;

      if (IsExplicitImplementation != otherMember.IsExplicitImplementation)
        return false;

      var mfv = Mfv;
      if (mfv == null)
        return false;

      if (!mfv.IsExtensionMember)
        return true;

      var otherSymbol = otherMember.Mfv;
      if (!(otherSymbol is { IsExtensionMember: true }))
        return false;

      var apparentEntity = mfv.ApparentEnclosingEntity;
      var otherApparentEntity = otherSymbol.ApparentEnclosingEntity;
      return apparentEntity.Equals(otherApparentEntity);
    }

    public override int GetHashCode() => ShortName.GetHashCode();
  }
}
