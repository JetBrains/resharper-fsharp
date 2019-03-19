using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpFieldBase<TDeclaration> : FSharpTypeMember<TDeclaration>, IField
    where TDeclaration : IFSharpDeclaration, ITypeMemberDeclaration, IModifiersOwnerDeclaration
  {
    protected FSharpFieldBase([NotNull] TDeclaration declaration) : base(declaration)
    {
    }

    [CanBeNull] protected abstract FSharpType FieldType { get; }

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.FIELD;

    public IType Type => GetType(FieldType);

    public ConstantValue ConstantValue =>
      ConstantValue.NOT_COMPILE_TIME_CONSTANT;

    public bool IsField => true;
    public bool IsConstant => false;
    public bool IsEnumMember => false;
    public int? FixedBufferSize => null;

    public override bool IsFSharpMember => false;
  }

  internal class FSharpTypePrivateField : FSharpFieldBase<TopPatternDeclarationBase>
  {
    public FSharpTypePrivateField([NotNull] TopPatternDeclarationBase declaration) : base(declaration)
    {
    }

    private FSharpMemberOrFunctionOrValue Field => Symbol as FSharpMemberOrFunctionOrValue;
    protected override FSharpType FieldType => Field?.FullType;
  }

  internal class FSharpValField : FSharpFieldBase<ValField>
  {
    public FSharpValField([NotNull] ValField declaration) : base(declaration)
    {
    }

    private FSharpField Field => Symbol as FSharpField;
    protected override FSharpType FieldType => Field?.FieldType;
  }
}
