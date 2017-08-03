using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  /// <summary>
  /// Field in a record or in a union case or in an exception
  /// </summary>
  internal class FSharpFieldProperty : FSharpFieldPropertyBase
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

    public override string ShortName => Field.Name;
    public override bool IsStatic => false;
    public override bool IsWritable => Field.IsMutable;
    public override IType ReturnType { get; }
  }
}