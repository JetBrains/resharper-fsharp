using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class PrimaryConstructorDeclaration
  {
    public override TreeTextRange GetNameRange() =>
      GetContainingTypeDeclaration()?.GetNameRange() ?? TreeTextRange.InvalidRange;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpPrimaryConstructor(this);
  }
}
