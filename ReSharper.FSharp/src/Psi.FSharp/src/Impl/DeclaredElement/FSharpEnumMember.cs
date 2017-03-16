using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal class FSharpEnumMember : FSharpTypeMember<FSharpEnumMemberDeclaration>, IField
  {
    public FSharpEnumMember([NotNull] IDeclaration declaration) : base(declaration)
    {
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
    public ConstantValue ConstantValue => ConstantValue.NOT_COMPILE_TIME_CONSTANT; // todo: calculate

    public override bool IsAbstract => false;
    public override bool IsSealed => false;
    public override bool IsVirtual => false;
    public override bool IsOverride => false;
    public override bool IsStatic => true;
    public override bool IsReadonly => true;
  }
}