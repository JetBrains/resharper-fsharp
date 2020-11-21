using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  internal class
    FSharpPropertyAccessorPointer : FSharpGeneratedElementPointerBase<FSharpGeneratedPropertyAccessor, IProperty>
  {
    private readonly bool myIsGetter;

    public FSharpPropertyAccessorPointer(FSharpGeneratedPropertyAccessor accessor, bool isGetter) : base(accessor)
    {
      myIsGetter = isGetter;
    }

    public override FSharpGeneratedPropertyAccessor CreateGenerated(IProperty property) =>
      new FSharpGeneratedPropertyAccessor(property, myIsGetter);
  }
}
