using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpEnumMember : FSharpTypeMember<EnumMemberDeclaration>, IField
  {
    public FSharpEnumMember([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    [CanBeNull] public FSharpField Field => Symbol as FSharpField;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.ENUM_MEMBER;

    public IType Type =>
      GetContainingType() is var typeElement && typeElement != null
        ? TypeFactory.CreateType(typeElement)
        : TypeFactory.CreateUnknownType(Module);

    public bool IsField => false;
    public bool IsConstant => false;
    public bool IsEnumMember => true;
    public int? FixedBufferSize => null;

    public ConstantValue ConstantValue =>
      Field?.LiteralValue?.Value is var literalValue && literalValue != null
        ? new ConstantValue(literalValue, Type)
        : ConstantValue.BAD_VALUE;

    public override bool IsAbstract => false;
    public override bool IsSealed => false;
    public override bool IsVirtual => false;
    public override bool IsOverride => false;
    public override bool IsStatic => true;
    public override bool IsReadonly => true;

    public override bool IsFSharpMember => false;
  }
}
