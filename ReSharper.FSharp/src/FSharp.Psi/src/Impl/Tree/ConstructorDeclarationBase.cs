using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class ConstructorDeclarationBase : FSharpProperTypeMemberDeclarationBase
  {
    protected override string DeclaredElementName =>
      IsStatic ? DeclaredElementConstants.STATIC_CONSTRUCTOR_NAME : DeclaredElementConstants.CONSTRUCTOR_NAME;

    public override string SourceName => DeclaredElementName;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpSecondaryConstructor(this);

    public override IFSharpIdentifierLikeNode NameIdentifier => null;
    public override TreeTextRange GetNameIdentifierRange() => GetNameRange();
  }
}
