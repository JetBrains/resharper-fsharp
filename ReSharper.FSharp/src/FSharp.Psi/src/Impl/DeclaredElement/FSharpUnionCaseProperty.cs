using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// <summary>
  /// A union case compiled to a static property.
  /// </summary>
  internal class FSharpUnionCaseProperty : FSharpFieldPropertyBase<SingletonCaseDeclaration>, IUnionCase
  {
    internal FSharpUnionCaseProperty([NotNull] ISingletonCaseDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() => ContainingType.GetRepresentationAccessRights();
    public AccessRights RepresentationAccessRights => ContainingType.GetFSharpRepresentationAccessRights();

    public override bool IsStatic => true;

    public override IType ReturnType =>
      GetContainingType() is var containingType && containingType != null
        ? TypeFactory.CreateType(containingType)
        : TypeFactory.CreateUnknownType(Module);
  }
}
