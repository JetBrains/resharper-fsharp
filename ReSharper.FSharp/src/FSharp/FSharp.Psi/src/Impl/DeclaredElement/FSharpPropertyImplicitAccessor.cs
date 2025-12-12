using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;

public class FSharpPropertyImplicitAccessor([NotNull] IOverridableMember owner, AccessorKind kind)
  : ImplicitAccessor(owner, kind)
{
  public override AccessRights GetAccessRights() =>
    OwnerMember.GetDeclarations().FirstOrDefault() is IAccessorOwnerDeclaration accessorOwner &&
    accessorOwner.GetAccessor(Kind)?.GetAccessRights() is { } accessRights
      ? accessRights
      : base.GetAccessRights();
}
