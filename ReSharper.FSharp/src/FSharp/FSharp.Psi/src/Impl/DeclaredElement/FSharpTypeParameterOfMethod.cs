using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpTypeParameterOfMethod([NotNull] IFunction method, [NotNull] string name, int index)
    : FSharpDeclaredElementBase, ITypeParameter
  {
    [NotNull] public readonly IFunction Method = method;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.TYPE_PARAMETER;

    public override string ShortName { get; } = name;

    public override bool IsValid() => Method.IsValid();
    public override IPsiModule Module => Method.Module;
    public override IPsiServices GetPsiServices() => Method.GetPsiServices();

    public override ITypeElement GetContainingType() => null;
    public override ITypeMember GetContainingTypeMember() => Method;

    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;
    public int TypeParametersCount => 0;
    public override ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

    public IClrTypeName GetClrName() => EmptyClrTypeName.Instance;
    public IList<IDeclaredType> GetSuperTypes() => SharedImplUtil.GetTypeParameterSuperTypes(this, IsValueType, TypeConstraints);
    public IList<ITypeElement> GetSuperTypeElements() => GetSuperTypes().ToTypeElements();

    public IEnumerable<ITypeMember> GetMembers() => EmptyList<ITypeMember>.InstanceList;
    public MemberPresenceFlag GetMemberPresenceFlag() => MemberPresenceFlag.NONE;

    public INamespace GetContainingNamespace() =>
      Method.ContainingType?.GetContainingNamespace() ??
      Module.GetSymbolScope(false).GlobalNamespace;

    public bool HasMemberWithName(string shortName, bool ignoreCase) => false;

    public IPsiSourceFile GetSingleOrDefaultSourceFile() => null;

    public IList<ITypeElement> NestedTypes => EmptyList<ITypeElement>.InstanceList;
    public IEnumerable<IField> Constants => EmptyList<IField>.Instance;
    public IEnumerable<IField> Fields => EmptyList<IField>.Instance;
    public IEnumerable<IConstructor> Constructors => EmptyList<IConstructor>.Instance;
    public IEnumerable<IOperator> Operators => EmptyList<IOperator>.Instance;
    public IEnumerable<IMethod> Methods => EmptyList<IMethod>.Instance;
    public IEnumerable<IProperty> Properties => EmptyList<IProperty>.Instance;
    public IEnumerable<IEvent> Events => EmptyList<IEvent>.Instance;
    public IEnumerable<string> MemberNames => EmptyList<string>.InstanceList;

    public TypeParameterNullability Nullability => TypeParameterNullability.Unknown;

    public TypeParameterNullability GetNullability(ISubstitution explicitInheritorSubstitution) =>
      TypeParameterNullability.Unknown;

    public int Index { get; } = index;
    public TypeParameterVariance Variance => TypeParameterVariance.INVARIANT;

    [CanBeNull]
    private FSharpGenericParameter GetFcsParameter()
    {
      if (!(Method is IFSharpTypeMember {ContainingType: { } typeElement, Symbol: FSharpMemberOrFunctionOrValue mfv})) 
        return null;

      var index = Index + typeElement.TypeParametersCount;
      var mfvTypeParameters = mfv.GenericParameters;
      return mfvTypeParameters.Count > index
        ? mfvTypeParameters[index]
        : null;
    }

    private IEnumerable<FSharpGenericParameterConstraint> FcsConstraints =>
        GetFcsParameter()?.Constraints ?? EmptyList<FSharpGenericParameterConstraint>.Instance;

    public bool IsValueType =>
      FcsConstraints.Any(constraint => constraint.IsNonNullableValueTypeConstraint);

    public bool IsReferenceType =>
      FcsConstraints.Any(constraint => constraint.IsReferenceTypeConstraint || constraint.IsSupportsNullConstraint);

    public bool IsUnmanagedType =>
      FcsConstraints.Any(constraint => constraint.IsUnmanagedConstraint);

    public bool HasDefaultConstructor =>
      FcsConstraints.Any(constraint => constraint.IsRequiresDefaultConstructorConstraint);

    public bool IsNotNullableValueOrReferenceType => false;

    public bool AllowsByRefLikeType => false;

    public bool HasTypeConstraints =>
      FcsConstraints.Any(constraint => constraint.IsCoercesToConstraint);

    public IList<IType> TypeConstraints =>
      FcsConstraints
        .SelectNotNull(constraint =>
          constraint.IsCoercesToConstraint ? constraint.CoercesToTarget.MapType(AllTypeParameters, Module) : null)
        .ToIList();

    private IList<ITypeParameter> AllTypeParameters =>
      Method is IFSharpTypeMember typeMember ? typeMember.AllTypeParameters : EmptyList<ITypeParameter>.Instance;

    public TypeParameterConstraintFlags Constraints
    {
      get
      {
        var result = TypeParameterConstraintFlags.None;
        foreach (var fcsConstraint in FcsConstraints)
        {
          if (fcsConstraint.IsReferenceTypeConstraint || fcsConstraint.IsSupportsNullConstraint)
            result |= TypeParameterConstraintFlags.ReferenceType;

          else if (fcsConstraint.IsNonNullableValueTypeConstraint)
            result |= TypeParameterConstraintFlags.ValueType;

          else if (fcsConstraint.IsRequiresDefaultConstructorConstraint)
            result |= TypeParameterConstraintFlags.Constructor;

          else if (fcsConstraint.IsUnmanagedConstraint)
            result |= TypeParameterConstraintFlags.Unmanaged;
        }

        return result;
      }
    }

    public NullableAnnotation NullableAnnotation => NullableAnnotation.Unknown;

    public IParametersOwner OwnerFunction => Method;
    public IMethod OwnerMethod => (IMethod) Method;
    public ITypeParametersOwner Owner => Method as ITypeParametersOwner;
    public ITypeElement OwnerType => Method.ContainingType;

    public override bool Equals(object obj) =>
      obj is FSharpTypeParameterOfMethod other &&
      ReferenceEquals(Method, other.Method) && Index == other.Index;

    public override int GetHashCode() => Index;
  }
}
