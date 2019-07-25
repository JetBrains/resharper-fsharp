using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// Base type for F# symbols compiled as properties.
  internal abstract class FSharpCompiledPropertyBase<T> : FSharpMemberBase<T>, IProperty
    where T : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    internal FSharpCompiledPropertyBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.PROPERTY;

    public string GetDefaultPropertyMetadataName() => ShortName;

    public IType Type => ReturnType;

    public InvocableSignature GetSignature(ISubstitution substitution) =>
      new InvocableSignature(this, substitution);

    public bool IsAuto => false;
    public bool IsDefault => false;
    public bool IsReadable => true;
    public virtual bool IsWritable => false;
    public IAccessor Getter => new ImplicitAccessor(this, AccessorKind.GETTER);
    public IAccessor Setter => IsWritable ? new ImplicitAccessor(this, AccessorKind.SETTER) : null;

    public override bool IsFSharpMember => true;
  }
}
