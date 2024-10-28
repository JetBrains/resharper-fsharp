using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeInherit
  {
    protected override FSharpSymbolReference CreateReference() => new(this);
    public override IFSharpIdentifier FSharpIdentifier => TypeName?.Identifier;

    public override FSharpReferenceContext? ReferenceContext => FSharpReferenceContext.Type;
  }
}
