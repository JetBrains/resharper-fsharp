using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// <summary>
  /// A union case compiled to a static property.
  /// </summary>
  internal class FSharpUnionCaseProperty : FSharpCompiledPropertyBase<IUnionCaseDeclaration>, IUnionCase
  {
    internal FSharpUnionCaseProperty([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() => GetContainingType().GetRepresentationAccessRights();
    public AccessRights RepresentationAccessRights => GetContainingType().GetFSharpRepresentationAccessRights();

    public override bool IsStatic => true;

    public override IType ReturnType =>
      GetContainingType() is var containingType && containingType != null
        ? TypeFactory.CreateType(containingType)
        : TypeFactory.CreateUnknownType(Module);
  }
}
