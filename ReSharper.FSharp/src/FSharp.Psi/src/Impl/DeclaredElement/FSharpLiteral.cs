using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpLiteral : FSharpTypeMember<TopPatternDeclarationBase>, IField, ITopLevelPatternDeclaredElement
  {
    public FSharpLiteral([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    [CanBeNull] public FSharpMemberOrFunctionOrValue Mfv => Symbol as FSharpMemberOrFunctionOrValue;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CONSTANT;

    public override bool IsStatic => true;

    public IType Type => GetType(Mfv?.FullType);

    public ConstantValue ConstantValue =>
      Mfv is { } mfv
        ? new ConstantValue(mfv.LiteralValue.Value, Type)
        : ConstantValue.BAD_VALUE;

    public bool IsField => false;
    public bool IsConstant => true;
    public bool IsEnumMember => false;
    public bool IsRequired => false;
    public ReferenceKind ReferenceKind => ReferenceKind.VALUE;
    public int? FixedBufferSize => null;
  }
}
