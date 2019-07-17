using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class CtorSelfId
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;
  }
}
