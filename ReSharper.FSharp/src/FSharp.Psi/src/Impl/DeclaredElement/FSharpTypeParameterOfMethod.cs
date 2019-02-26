using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpTypeParameterOfMethod : FSharpDeclaredElementBase, ITypeParameter
  {
    [NotNull] public readonly ITypeMember Method;

    public FSharpTypeParameterOfMethod([NotNull] ITypeMember method, [NotNull] string name, int index)
    {
      Method = method;
      Index = index;
      ShortName = name;
    }

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.TYPE_PARAMETER;

    public override string ShortName { get; }

    public override bool IsValid() => Method.IsValid();
    public override IPsiModule Module => Method.Module;
    public override IPsiServices GetPsiServices() => Method.GetPsiServices();

    public override ITypeElement GetContainingType() => null;
    public override ITypeMember GetContainingTypeMember() => Method;

    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;
    public override ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

    public IClrTypeName GetClrName() => EmptyClrTypeName.Instance;
    public IList<IDeclaredType> GetSuperTypes() => EmptyList<IDeclaredType>.Instance;
    public IEnumerable<ITypeMember> GetMembers() => EmptyList<ITypeMember>.InstanceList;

    public INamespace GetContainingNamespace() =>
      Method.GetContainingType()?.GetContainingNamespace() ??
      Module.GetSymbolScope().GlobalNamespace;

    public IPsiSourceFile GetSingleOrDefaultSourceFile() => null;

    public IList<ITypeElement> NestedTypes => EmptyList<ITypeElement>.InstanceList;
    public IEnumerable<IConstructor> Constructors => EmptyList<IConstructor>.Instance;
    public IEnumerable<IOperator> Operators => EmptyList<IOperator>.Instance;
    public IEnumerable<IMethod> Methods => EmptyList<IMethod>.Instance;
    public IEnumerable<IProperty> Properties => EmptyList<IProperty>.Instance;
    public IEnumerable<IEvent> Events => EmptyList<IEvent>.Instance;
    public IEnumerable<string> MemberNames => EmptyList<string>.InstanceList;

    public int Index { get; }
    public TypeParameterVariance Variance => TypeParameterVariance.INVARIANT;

    public bool IsValueType => false; // todo
    public bool IsClassType => false; // todo
    public bool IsUnmanagedType => false;
    public bool HasDefaultConstructor => false;
    public IList<IType> TypeConstraints => EmptyList<IType>.Instance;
    public TypeParameterConstraintsMask ConstraintsMask => default; // todo

    public IMethod OwnerMethod => (IMethod) Method;
    public ITypeParametersOwner Owner => Method as ITypeParametersOwner;
    public ITypeElement OwnerType => Method.GetContainingType();

    public override bool Equals(object obj) =>
      obj is FSharpTypeParameterOfMethod other &&
      ReferenceEquals(Method, other.Method) && Index == other.Index;

    public override int GetHashCode() => Index;
  }
}
