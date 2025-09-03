using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public abstract class FSharpGeneratedElementPointerBase<TGenerated, TOrigin>(TGenerated element)
    : IDeclaredElementPointer<TGenerated>
    where TGenerated : class, IFSharpGeneratedFromOtherElement
    where TOrigin : class, IClrDeclaredElement
  {
    [CanBeNull]
    private IDeclaredElementPointer<IClrDeclaredElement> ElementPointer { get; } =
      element.OriginElement?.CreateElementPointer();

    public TGenerated FindDeclaredElement() =>
      ElementPointer?.FindDeclaredElement() is TOrigin origin
        ? CreateGenerated(origin)
        : null;

    [CanBeNull]
    public abstract TGenerated CreateGenerated(TOrigin fsElement);
  }
}
