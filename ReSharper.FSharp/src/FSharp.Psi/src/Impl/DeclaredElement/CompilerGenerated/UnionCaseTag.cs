using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class UnionCaseTag : FSharpGeneratedMemberBase, IField, IFSharpGeneratedFromOtherElement
  {
    private IUnionCase UnionCase { get; }

    public UnionCaseTag(IUnionCase unionCase) =>
      UnionCase = unionCase;

    public override string ShortName => UnionCase.ShortName;

    private ITypeElement Union => UnionCase.GetContainingType();
    private FSharpUnionTagsClass TagsClass => Union.GetUnionTagsClass();

    IClrDeclaredElement IFSharpGeneratedFromOtherElement.OriginElement => UnionCase;

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new UnionCaseTagPointer(this);

    public int Index => Union.GetUnionCases().IndexOf(UnionCase);

    protected override IClrDeclaredElement ContainingElement => TagsClass;
    public override ITypeElement GetContainingType() => TagsClass;
    public override ITypeMember GetContainingTypeMember() => TagsClass;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CONSTANT;

    public IType Type => PredefinedType.Int;

    public ConstantValue ConstantValue =>
      Index is var index && index != -1
        ? new ConstantValue(index, Type)
        : ConstantValue.BAD_VALUE;

    public bool IsField => false;
    public bool IsConstant => true;
    public bool IsEnumMember => false;
    public int? FixedBufferSize => null;

    public override bool IsStatic => true;
    public override bool IsReadonly => true;

    public override ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

    public override bool IsValid() =>
      UnionCase.IsValid();

    public override bool Equals(object obj) =>
      obj is UnionCaseTag other && Equals(UnionCase, other.UnionCase);

    public override int GetHashCode() =>
      UnionCase.GetHashCode();
  }
}
