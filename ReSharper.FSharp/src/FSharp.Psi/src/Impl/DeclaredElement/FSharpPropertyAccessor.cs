using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpPropertyAccessor : FSharpMethodBase<AccessorDeclaration>, IAccessor
  {
    public FSharpPropertyAccessor(ITypeMemberDeclaration declaration)
      : base(declaration)
    {
    }

    public IOverridableMember OwnerMember => GetDeclaration()?.OwnerMember?.DeclaredElement as IProperty;
    public AccessorKind Kind => GetDeclaration()?.Kind ?? AccessorKind.UNKNOWN;
    public bool IsInitOnly => false;
    public IParameter ValueVariable => Kind == AccessorKind.SETTER ? Parameters.Last() : null;
    public override bool IsVisibleFromFSharp => false;
    public override IList<ITypeParameter> AllTypeParameters => GetContainingType().GetAllTypeParametersReversed();

    public override bool Equals(object obj) =>
      obj is FSharpPropertyAccessor accessor && ShortName == accessor.ShortName && base.Equals(accessor);

    public override int GetHashCode() => ShortName.GetHashCode();
  }
}
