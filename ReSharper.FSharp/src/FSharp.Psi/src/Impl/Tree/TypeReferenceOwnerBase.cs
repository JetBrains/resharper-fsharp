using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class TypeReferenceOwnerBase : ReferenceOwnerBase
  {
    protected override FSharpSymbolReference CreateReference() =>
      new TypeReference(this);
  }

  internal abstract class BaseTypeReferenceOwnerBase : TypeReferenceOwnerBase
  {
    protected override FSharpSymbolReference CreateReference() =>
      new BaseTypeReference(this);
  }
}
