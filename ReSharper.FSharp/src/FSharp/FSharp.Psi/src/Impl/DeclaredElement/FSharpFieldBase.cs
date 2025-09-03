using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpFieldBase<TDeclaration>([NotNull] TDeclaration declaration)
    : FSharpTypeMember<TDeclaration>(declaration), IField
    where TDeclaration : IFSharpDeclaration, ITypeMemberDeclaration, IModifiersOwnerDeclaration
  {
    [CanBeNull] protected abstract FSharpType FieldType { get; }

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.FIELD;

    public IType Type => GetType(FieldType);

    public ConstantValue ConstantValue =>
      ConstantValue.NOT_COMPILE_TIME_CONSTANT;

    public bool IsField => true;
    public bool IsConstant => false;
    public bool IsEnumMember => false;
    public bool IsRequired => false;
    public ReferenceKind ReferenceKind => ReferenceKind.VALUE;
    public int? FixedBufferSize => null;
    
    public void SetIsMutable(bool value)
    {
      foreach (var decl in GetDeclarations())
        if (decl is IFSharpMutableModifierOwnerDeclaration mutableModifierOwnerDecl)
          mutableModifierOwnerDecl.SetIsMutable(value);
    }
  }

  internal class FSharpTypePrivateField([NotNull] TopPatternDeclarationBase declaration)
    : FSharpFieldBase<TopPatternDeclarationBase>(declaration), IMutableModifierOwner,
      ITypePrivateMember, ITopLevelPatternDeclaredElement
  {
    private FSharpMemberOrFunctionOrValue Field => Symbol as FSharpMemberOrFunctionOrValue;
    protected override FSharpType FieldType => Field?.FullType;

    public override AccessRights GetAccessRights() => AccessRights.INTERNAL;

    public bool IsMutable =>
      GetDeclaration() is ITopReferencePat { IsMutable: true };

    public bool CanBeMutable =>
      GetDeclaration() is ITopReferencePat { CanBeMutable: true };

    public override bool IsStatic =>
      GetDeclaration() is { IsStatic: true };
  }

  internal class FSharpValField([NotNull] ValFieldDeclaration declaration)
    : FSharpFieldBase<ValFieldDeclaration>(declaration), IMutableModifierOwner
  {
    private FSharpField Field => Symbol as FSharpField;
    protected override FSharpType FieldType => Field?.FieldType;

    public bool IsMutable =>
      GetDeclaration() is { IsMutable: true };

    public bool CanBeMutable => true;
  }
}
