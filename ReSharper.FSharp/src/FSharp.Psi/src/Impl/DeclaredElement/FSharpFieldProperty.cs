using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// Union case or exception field compiled to a property.
  internal class FSharpUnionCaseField<T> : FSharpFieldProperty<T>
    where T : IFSharpDeclaration, IModifiersOwnerDeclaration, ICaseFieldDeclaration, ITypeMemberDeclaration
  {
    internal FSharpUnionCaseField([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsVisibleFromFSharp => false;
    public override bool CanNavigateTo => true;
  }

  /// Record field compiled to a property.
  internal class FSharpRecordField : FSharpFieldProperty<RecordFieldDeclaration>, IRecordField
  {
    private readonly bool myIsMutable;

    internal FSharpRecordField([NotNull] IRecordFieldDeclaration declaration) : base(declaration) =>
      myIsMutable = declaration.IsMutable;

    public bool IsMutable =>
      myIsMutable || GetContainingType().IsCliMutableRecord();

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

    public IParameter GetGeneratedParameter() =>
      new FSharpGeneratedParameter(GetContainingType().GetGeneratedConstructor(), this);
  }

  public interface IRecordField : IProperty, IRepresentationAccessRightsOwner, IMutableModifierOwner
  {
  }
}
