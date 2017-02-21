using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpNestedModule : FSharpModule
  {
    public FSharpNestedModule([NotNull] IClassPart part) : base(part)
    {
    }

    protected override bool AcceptsPart(TypePart part)
    {
      return part is NestedModulePart;
    }
  }
}