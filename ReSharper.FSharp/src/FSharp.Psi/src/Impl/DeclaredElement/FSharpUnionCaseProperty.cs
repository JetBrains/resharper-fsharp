using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// <summary>
  /// Union case compiled to property
  /// </summary>
  internal class FSharpUnionCaseProperty : FSharpFieldPropertyBase<SingletonCaseDeclaration>, IUnionCase
  {
    [NotNull]
    public FSharpUnionCase UnionCase { get; }

    internal FSharpUnionCaseProperty([NotNull] ISingletonCaseDeclaration declaration,
      [NotNull] FSharpUnionCase unionCase)
      : base(declaration)
    {
      UnionCase = unionCase;

      var containingType = declaration.GetContainingTypeDeclaration()?.DeclaredElement;
      ReturnType = containingType != null
        ? TypeFactory.CreateType(containingType)
        : TypeFactory.CreateUnknownType(Module);
    }

    public override string ShortName => UnionCase.Name;

    public override AccessRights GetAccessRights() =>
      GetContainingType() is TypeElement typeElement
        ? typeElement.GetRepresentationAccessRights()
        : AccessRights.NONE;

    public override bool IsStatic => true;
    public override bool IsWritable => false;
    public override IType ReturnType { get; }
  }
}