using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class TypeReferenceOwnerBase : ReferenceOwnerBase
  {
    protected override FSharpSymbolReference CreateReference() =>
      new TypeReference(this);

    public override FSharpReferenceContext? ReferenceContext => FSharpReferenceContext.Type;
  }
}
