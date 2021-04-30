using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopReferencePat
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override TreeTextRange GetNameRange() => NameIdentifier.GetNameRange();

    public override IFSharpIdentifierLikeNode NameIdentifier => ReferenceName?.Identifier;

    public override TreeTextRange GetNameIdentifierRange() =>
      NameIdentifier.GetMemberNameIdentifierRange();

    public TreeNodeCollection<IAttribute> Attributes =>
      this.GetBinding()?.Attributes ??
      TreeNodeCollection<IAttribute>.Empty;
  }
}
