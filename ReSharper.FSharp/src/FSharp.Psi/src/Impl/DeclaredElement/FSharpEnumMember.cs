using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpEnumMember : FSharpTypeMember<EnumMemberDeclaration>, IField
  {
    [NotNull]
    public FSharpField Field { get; }

    public FSharpEnumMember([NotNull] ITypeMemberDeclaration declaration, [NotNull] FSharpField field) :
      base(declaration)
    {
      Field = field;
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.ENUM_MEMBER;
    }

    public IType Type
    {
      get
      {
        var enumType = GetContainingType();
        return enumType != null
          ? TypeFactory.CreateType(enumType)
          : TypeFactory.CreateUnknownType(Module);
      }
    }

    public bool IsField => false;
    public bool IsConstant => false;
    public bool IsEnumMember => true;
    public int? FixedBufferSize => null;

    public ConstantValue ConstantValue =>
      Field.LiteralValue != null
        ? new ConstantValue(Field.LiteralValue.Value, Type)
        : ConstantValue.BAD_VALUE;

    public override bool IsAbstract => false;
    public override bool IsSealed => false;
    public override bool IsVirtual => false;
    public override bool IsOverride => false;
    public override bool IsStatic => true;
    public override bool IsReadonly => true;

    public override bool IsMember => false;
  }
}