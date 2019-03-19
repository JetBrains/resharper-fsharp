using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class TypeExpressionBase : ReferenceExpressionBase
  {
    protected override FSharpSymbolReference CreateReference() =>
      new TypeReference(this);
  }
}
