using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpValField<TDeclaration> : FSharpTypeMember<TDeclaration>, IField
    where TDeclaration : FSharpDeclarationBase, ITypeMemberDeclaration, IFSharpDeclaration, 
    IAccessRightsOwnerDeclaration, IModifiersOwnerDeclaration
  {
    [NotNull]
    public FSharpType FieldType { get; }

    public FSharpValField([NotNull] TDeclaration declaration, [NotNull] FSharpType fieldType) : base(declaration) =>
      FieldType = fieldType;

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.FIELD;

    public IType Type
    {
      get
      {
        var declaration = GetDeclaration();
        if (declaration == null)
          return TypeFactory.CreateUnknownType(Module);

        return FSharpTypesUtil.GetType(FieldType, declaration, Module) ??
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