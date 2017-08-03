using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  /// <summary>
  /// Union case compiled to property
  /// </summary>
  internal class FSharpUnionCaseProperty : FSharpFieldPropertyBase
  {
    [NotNull]
    public FSharpUnionCase UnionCase { get; }

    internal FSharpUnionCaseProperty([NotNull] IFieldDeclaration declaration, [NotNull] FSharpUnionCase unionCase)
      : base(declaration)
    {
      UnionCase = unionCase;

      var containingType = declaration.GetContainingTypeDeclaration()?.DeclaredElement;
      ReturnType = containingType != null
        ? TypeFactory.CreateType(containingType)
        : TypeFactory.CreateUnknownType(Module);
    }

    public override string ShortName => UnionCase.Name;
    public override bool IsStatic => true;
    public override bool IsWritable => false;
    public override IType ReturnType { get; }
  }
}