using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// Union case or exception field compiled to a property.
  internal class FSharpUnionCaseField<T> : FSharpFieldProperty<T>, IUnionCaseField
    where T : IFSharpDeclaration, IModifiersOwnerDeclaration, ICaseFieldDeclaration, ITypeMemberDeclaration
  {
    internal FSharpUnionCaseField([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsVisibleFromFSharp => false;
    public override bool CanNavigateTo => true;

    protected override ITypeElement GetTypeElement(IDeclaration declaration)
    {
      var unionCaseDecl = declaration.GetContainingNode<ITypeDeclaration>();
      return unionCaseDecl?.DeclaredElement ?? unionCaseDecl?.GetContainingNode<ITypeDeclaration>()?.DeclaredElement;
    }

    public override IParameter GetGeneratedParameter()
    {
      var decl = GetDeclaration();
      if (decl == null)
        return null;

      var caseWithFields = decl.GetContainingNode<IDeclaration>()?.DeclaredElement as IFSharpUnionCase;
      var constructor = caseWithFields?.GetConstructor();
      return constructor != null
        ? new FSharpGeneratedParameterFromUnionCaseField(constructor, this, Index)
        : null;
    }

    public int Index => GetDeclaration()?.Index ?? -1;
  }

  /// Record field compiled to a property.
  internal class FSharpRecordField : FSharpFieldProperty<RecordFieldDeclaration>, IFSharpRecordField
  {
    internal FSharpRecordField([NotNull] IRecordFieldDeclaration declaration) : base(declaration)
    {
    }

    public bool IsMutable =>
      GetDeclaration() is { IsMutable: true } || GetContainingType().IsCliMutableRecord();

    public void SetIsMutable(bool value)
    {
      foreach (var declaration in GetDeclarations())
        if (declaration is IRecordFieldDeclaration valFieldDeclaration)
          valFieldDeclaration.SetIsMutable(value);
    }

    public bool CanBeMutable => true;

    public override bool IsWritable => IsMutable;

    public override AccessRights GetAccessRights() => GetContainingType().GetRepresentationAccessRights();
    public AccessRights RepresentationAccessRights => GetContainingType().GetFSharpRepresentationAccessRights();
  }

  internal abstract class FSharpFieldProperty<T> : FSharpCompiledPropertyBase<T>, IFSharpFieldProperty
    where T : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    internal FSharpFieldProperty([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    // todo: named case fields have FSharpParameter symbols in resolve cache
    [CanBeNull] public FSharpField Field => Symbol as FSharpField;
    [CanBeNull] protected virtual FSharpType FieldType => Field?.FieldType;

    public override IType ReturnType => GetType(FieldType);

    public override AccessRights GetAccessRights() =>
      GetContainingType().GetRepresentationAccessRights();

    public virtual IParameter GetGeneratedParameter() =>
      GetContainingType().GetGeneratedConstructor() is { } constructor
        ? new FSharpGeneratedParameter(constructor, this, false)
        : null;

    public int Index => throw new System.NotImplementedException();
  }
}
