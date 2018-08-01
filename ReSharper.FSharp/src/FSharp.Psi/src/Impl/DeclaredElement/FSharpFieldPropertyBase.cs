using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpFieldPropertyBase<T> : FSharpTypeMember<T>, IProperty
    where T : FSharpDeclarationBase, IModifiersOwnerDeclaration
  {
    internal FSharpFieldPropertyBase([NotNull] ITypeMemberDeclaration declaration)
      : base(declaration)
    {
    }

    public abstract override string ShortName { get; }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.PROPERTY;

    public bool CanBeImplicitImplementation => false;
    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;

    public string GetDefaultPropertyMetadataName() => ShortName; // todo: check this
    public InvocableSignature GetSignature(ISubstitution substitution) => new InvocableSignature(this, substitution);

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      EmptyList<IParametersOwnerDeclaration>.Instance;

    public IType Type => ReturnType;
    public abstract IType ReturnType { get; }
    public IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public ReferenceKind ReturnKind => ReferenceKind.VALUE;

    public bool IsAuto => false;
    public bool IsDefault => false;
    public bool IsReadable => true;
    public abstract bool IsWritable { get; }
    public IAccessor Getter => new ImplicitAccessor(this, AccessorKind.GETTER);
    public IAccessor Setter => IsWritable ? new ImplicitAccessor(this, AccessorKind.SETTER) : null;

    public override bool IsMember => true;
  }
}