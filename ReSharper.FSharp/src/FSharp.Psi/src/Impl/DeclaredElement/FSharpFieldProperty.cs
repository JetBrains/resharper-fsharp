using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// <summary>
  /// Field in a record or in a union case or in an exception
  /// </summary>
  internal class FSharpFieldProperty : FSharpFieldPropertyBase<FieldDeclaration>
  {
    [NotNull]
    public FSharpField Field { get; }

    internal FSharpFieldProperty([NotNull] IFieldDeclaration declaration, [NotNull] FSharpField field)
      : base(declaration)
    {
      Field = field;
      ReturnType = FSharpTypesUtil.GetType(field.FieldType, declaration, Module) ??
                   TypeFactory.CreateUnknownType(Module);
    }

    public override bool IsVisibleFromFSharp => !Field.IsNameGenerated;

    public override IType ReturnType { get; }
    public override string ShortName => Field.Name;
    public override bool IsStatic => false;

    public override bool IsWritable =>
      Field.IsMutable || GetContainingType() is TypeElement typeElement && typeElement.IsCliMutableRecord();
  }
}
