using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NamedSelfId
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;
  }
}
