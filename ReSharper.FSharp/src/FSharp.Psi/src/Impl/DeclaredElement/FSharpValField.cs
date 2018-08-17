using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpValField : FSharpTypeMember<ValField>, IField
  {
    [NotNull]
    public FSharpField Field { get; }

    public FSharpValField([NotNull] ValField declaration, [NotNull] FSharpField field) : base(declaration) =>
      Field = field;

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.FIELD;
    }

    public IType Type
    {
      get
      {
        var declaration = GetDeclaration();
        if (declaration == null)
          return TypeFactory.CreateUnknownType(Module);

        return FSharpTypesUtil.GetType(Field.FieldType, declaration, Module) ??
               TypeFactory.CreateUnknownType(Module);
      }
    }

    public ConstantValue ConstantValue => ConstantValue.NOT_COMPILE_TIME_CONSTANT;
    public bool IsField => true;
    public bool IsConstant => false;
    public bool IsEnumMember => false;
    public int? FixedBufferSize => null;
    public override bool IsMember => false;
  }
}