using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class MemberConstructorDeclaration
  {
    protected override string DeclaredElementName =>
      GetContainingTypeDeclaration()?.CompiledName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override string SourceName =>
      GetContainingTypeDeclaration()?.SourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetNameRange() => NewKeyword.GetTreeTextRange();
    public override IFSharpIdentifierLikeNode NameIdentifier => null;

    public override TreeTextRange GetNameIdentifierRange() => GetNameRange();

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpConstructor(this);
  }
}
