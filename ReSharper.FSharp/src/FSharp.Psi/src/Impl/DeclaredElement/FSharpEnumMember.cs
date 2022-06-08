using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpEnumMember : FSharpTypeMember<EnumCaseDeclaration>, IField
  {
    public FSharpEnumMember([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    [CanBeNull] public FSharpField Field => Symbol as FSharpField;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.ENUM_MEMBER;

    public IType Type =>
      GetContainingType() is { } typeElement
        ? TypeFactory.CreateType(typeElement)
        : TypeFactory.CreateUnknownType(Module);

    public bool IsField => false;
    public bool IsConstant => false;
    public bool IsEnumMember => true;
    public ReferenceKind ReferenceKind => ReferenceKind.VALUE;
    public int? FixedBufferSize => null;

    public ConstantValue ConstantValue =>
      Field?.LiteralValue?.Value is { } literalValue
        ? new ConstantValue(literalValue, Type)
        : ConstantValue.BAD_VALUE;

    public override bool IsStatic => true;
    public override bool IsReadonly => true;

    public bool IsRequired => false;
  }
}
