using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class PrimaryConstructorDeclaration
  {
    public override TreeTextRange GetNameRange() =>
      GetContainingTypeDeclaration()?.GetNameRange() ?? TreeTextRange.InvalidRange;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpPrimaryConstructor(this);

    public ITokenNode EqualsToken => null;

    TreeNodeCollection<IFSharpPattern> IParameterOwnerMemberDeclaration.ParameterPatterns =>
      new(new[] {ParameterPatterns});
  }
}
