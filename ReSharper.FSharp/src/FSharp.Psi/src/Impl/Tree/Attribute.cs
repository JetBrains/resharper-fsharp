using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class Attribute
  {
    protected override FSharpSymbolReference CreateReference() =>
      new CtorReference(this);

    public override IFSharpIdentifier FSharpIdentifier => ReferenceName?.Identifier;
  }
}
