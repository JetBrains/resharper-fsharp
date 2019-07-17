using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class MemberSelfId
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;
  }
}
