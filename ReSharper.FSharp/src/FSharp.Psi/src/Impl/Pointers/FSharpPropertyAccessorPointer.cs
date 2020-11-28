using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  internal class
    FSharpPropertyAccessorPointer : FSharpGeneratedElementPointerBase<FSharpPropertyAccessorMethod, IFSharpProperty>
  {
    private readonly AccessorKind myKind;

    public FSharpPropertyAccessorPointer(FSharpPropertyAccessorMethod accessorMethod) : base(
      accessorMethod)
    {
      myKind = accessorMethod.Kind;
    }

    public override FSharpPropertyAccessorMethod CreateGenerated(IFSharpProperty property) =>
      new FSharpPropertyAccessorMethod(property, myKind);
  }
}
