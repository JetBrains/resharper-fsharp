using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopParametersOwnerPat
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override string SourceName => IsDeclaration ? base.SourceName : SharedImplUtil.MISSING_DECLARATION_NAME;
    public override TreeTextRange GetNameRange() => IsDeclaration ? base.GetNameRange() : TreeTextRange.InvalidRange;

    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    protected override IDeclaredElement CreateDeclaredElement() =>
      IsDeclaration ? base.CreateDeclaredElement() : null;

    public TreeNodeCollection<IAttribute> Attributes =>
      this.GetBinding()?.AllAttributes ??
      TreeNodeCollection<IAttribute>.Empty;
  }
}
