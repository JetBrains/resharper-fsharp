using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// <summary>
  /// Union case or exception field compiled to a property.
  /// </summary>
  internal class FSharpUnionCaseField<T> : FSharpFieldProperty<T>
    where T : FSharpDeclarationBase, IModifiersOwnerDeclaration, ICaseFieldDeclaration
  {
    internal FSharpUnionCaseField([NotNull] ITypeMemberDeclaration declaration, [NotNull] FSharpField field)
      : base(declaration, field)
    {
    }

    public override bool IsVisibleFromFSharp => false;
    public override bool CanNavigateTo => true;
  }


  /// <summary>
  /// Record field compiled to a property.
  /// </summary>
  internal class FSharpRecordField : FSharpFieldProperty<RecordFieldDeclaration>
  {
    internal FSharpRecordField([NotNull] ITypeMemberDeclaration declaration, [NotNull] FSharpField field)
      : base(declaration, field)
    {
    }

    public override bool IsWritable =>
      Field.IsMutable || GetContainingType() is TypeElement typeElement && typeElement.IsCliMutableRecord();
  }


  internal class FSharpFieldProperty<T> : FSharpFieldPropertyBase<T>
    where T : FSharpDeclarationBase, IModifiersOwnerDeclaration
  {
    [NotNull]
    public FSharpField Field { get; }

    internal FSharpFieldProperty([NotNull] ITypeMemberDeclaration declaration, [NotNull] FSharpField field)
      : base(declaration)
    {
      Field = field;
      ReturnType = FSharpTypesUtil.GetType(field.FieldType, declaration, Module) ??
                   TypeFactory.CreateUnknownType(Module);
    }

    public override IType ReturnType { get; }

    public override AccessRights GetAccessRights() =>
      GetContainingType() is TypeElement typeElement
        ? typeElement.GetRepresentationAccessRights()
        : AccessRights.PUBLIC;
  }
}
