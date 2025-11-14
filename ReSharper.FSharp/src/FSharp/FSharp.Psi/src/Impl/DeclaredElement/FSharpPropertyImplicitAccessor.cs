using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;

public class FSharpPropertyImplicitAccessor([NotNull] IOverridableMember owner, AccessorKind kind, AccessRights accessRights)
  : ImplicitAccessor(owner, kind)
{
  public override AccessRights GetAccessRights() => accessRights;
}
