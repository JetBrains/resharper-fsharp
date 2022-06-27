using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpGenerativeProvidedField : FSharpGenerativeProvidedMember<ProvidedFieldInfo>, IField
  {
    public FSharpGenerativeProvidedField(ProvidedFieldInfo info, ITypeElement containingType) :
      base(info, containingType)
    {
    }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.FIELD;

    public IType Type => Info.FieldType.MapType(Module);

    public ConstantValue ConstantValue =>
      IsConstant ? ConstantValue.Create(Info.GetRawConstantValue(), Type) : ConstantValue.BAD_VALUE;

    public bool IsConstant => Info.IsLiteral;
    public bool IsField => true;
    public bool IsEnumMember => false;
    public ReferenceKind ReferenceKind => ReferenceKind.VALUE;
    public bool IsRequired => false;
    public int? FixedBufferSize => null;
  }
}
