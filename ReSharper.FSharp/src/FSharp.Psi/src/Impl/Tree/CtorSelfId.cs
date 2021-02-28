using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class CtorSelfId
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    public override IType Type =>
      // FCS returns FSharpRef<TContainingType> for self identifiers.
      GetContainingType() is { } typeElement
        ? TypeFactory.CreateType(typeElement)
        : TypeFactory.CreateUnknownType(Module);
  }
}
