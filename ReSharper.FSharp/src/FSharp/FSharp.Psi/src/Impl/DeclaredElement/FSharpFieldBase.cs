using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

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
    public bool IsRequired => false;
    public ReferenceKind ReferenceKind => ReferenceKind.VALUE;
    public int? FixedBufferSize => null;
  }

  internal class FSharpTypePrivateField : FSharpFieldBase<TopPatternDeclarationBase>, IMutableModifierOwner,
    ITypePrivateMember, ITopLevelPatternDeclaredElement
  {
    public FSharpTypePrivateField([NotNull] TopPatternDeclarationBase declaration) : base(declaration)
    {
    }

    private FSharpMemberOrFunctionOrValue Field => Symbol as FSharpMemberOrFunctionOrValue;
    protected override FSharpType FieldType => Field?.FullType;

    public override AccessRights GetAccessRights() => AccessRights.INTERNAL;

    public bool IsMutable =>
      GetDeclaration() is ITopReferencePat { IsMutable: true };

    public void SetIsMutable(bool value)
    {
      foreach (var declaration in GetDeclarations())
        if (declaration is ITopReferencePat referencePat)
          referencePat.SetIsMutable(true);
    }

    public bool CanBeMutable =>
      GetDeclaration() is ITopReferencePat { CanBeMutable: true };

    public override bool IsStatic =>
      GetDeclaration() is { IsStatic: true };
  }

  internal class FSharpValField : FSharpFieldBase<ValFieldDeclaration>
  {
    public FSharpValField([NotNull] ValFieldDeclaration declaration) : base(declaration)
    {
    }

    private FSharpField Field => Symbol as FSharpField;
    protected override FSharpType FieldType => Field?.FieldType;
  }
}
