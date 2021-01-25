using System.Collections.Generic;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpFunctionBase<TDeclaration> : FSharpMemberBase<TDeclaration>, IFSharpFunction
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpFunctionBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override IList<IParameter> Parameters => this.GetParameters(Mfv);

    public InvocableSignature GetSignature(ISubstitution substitution) =>
      new InvocableSignature(this, substitution);

    public virtual IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public override IType ReturnType =>
      Mfv?.ReturnParameter.Type is { } returnType
        ? returnType.MapType(AllTypeParameters, Module, true, true) // todo: isFromMethod?
        : TypeFactory.CreateUnknownType(Module);

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!base.Equals(obj))
        return false;

      if (!(obj is IFSharpFunction fsFunction) || IsStatic != fsFunction.IsStatic) // RIDER-11321, RSRP-467025
        return false;

      return SignatureComparers.Strict.CompareWithoutName(GetSignature(IdSubstitution),
        fsFunction.GetSignature(fsFunction.IdSubstitution));
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public bool IsPredefined => false;
    public bool IsIterator => false;

    public IAttributesSet ReturnTypeAttributes =>
      new FSharpAttributeSet(Mfv?.ReturnParameter.Attributes ?? EmptyList<FSharpAttribute>.Instance, Module);
  }
}
