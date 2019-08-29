using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class CtorSelfId
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    public override IType Type =>
      // FCS returns FSharpRef<TContainingType> for self identifiers.
      GetContainingType() is ITypeElement typeElement
        ? TypeFactory.CreateType(typeElement)
        : TypeFactory.CreateUnknownType(Module);
  }
}
