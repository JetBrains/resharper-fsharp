using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedPropertyBase : FSharpGeneratedMemberBase, IProperty
  {
    protected FSharpGeneratedPropertyBase(ITypeElement typeElement) =>
      TypeElement = typeElement;

    protected override IClrDeclaredElement ContainingElement => TypeElement;
    public override ITypeElement GetContainingType() => TypeElement;
    public override ITypeMember GetContainingTypeMember() => (ITypeMember) TypeElement;

    [NotNull]
    public ITypeElement TypeElement { get; }

    public abstract IType Type { get; }

    public IType ReturnType => Type;
    public ReferenceKind ReturnKind => ReferenceKind.VALUE;

    public bool IsReadable => true;

    public IAccessor Getter =>
      new ImplicitAccessor(this, AccessorKind.GETTER);

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.PROPERTY;

    public bool IsExplicitImplementation => false;
    public bool CanBeImplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;

    public InvocableSignature GetSignature(ISubstitution substitution) =>
      new InvocableSignature(this, substitution);

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      EmptyList<IParametersOwnerDeclaration>.Instance;

    public string GetDefaultPropertyMetadataName() => ShortName;
    public IList<IParameter> Parameters => EmptyList<IParameter>.Instance;

    public IAccessor Setter => null;
    public bool IsWritable => false;
    public bool IsAuto => false;
    public bool IsDefault => false;

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!(obj is IFSharpTypeMember member)) return false;

      if (!ShortName.Equals(member.ShortName))
        return false;

      return Equals(GetContainingType(), member.GetContainingType());
    }

    public override int GetHashCode() => ShortName.GetHashCode();
  }
}
