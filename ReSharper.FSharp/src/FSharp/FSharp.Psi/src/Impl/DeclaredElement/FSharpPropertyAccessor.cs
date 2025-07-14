using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpPropertyAccessor(ITypeMemberDeclaration declaration)
    : FSharpMethodBase<AccessorDeclaration>(declaration), IFSharpExplicitAccessor
  {
    public IClrDeclaredElement OriginElement => GetDeclaration()?.OwnerMember.DeclaredElement;
    public AccessorKind Kind => GetDeclaration()?.Kind ?? AccessorKind.UNKNOWN;
    public override bool IsVisibleFromFSharp => false;
    public override IList<ITypeParameter> AllTypeParameters => GetContainingType().GetAllTypeParametersReversed();
    public override IList<IDeclaration> GetDeclarations() => [GetDeclaration()];

    public override bool Equals(object obj) =>
      obj is FSharpPropertyAccessor accessor && base.Equals(accessor);

    public override int GetHashCode() => ShortName.GetHashCode();
  }
}
