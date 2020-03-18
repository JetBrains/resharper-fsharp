using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeInherit
  {
    protected override FSharpSymbolReference CreateReference()
    {
      return new FSharpSymbolReference(this);
    }

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(Reference);

    public override IFSharpIdentifier FSharpIdentifier => TypeName?.Identifier;
  }
}
