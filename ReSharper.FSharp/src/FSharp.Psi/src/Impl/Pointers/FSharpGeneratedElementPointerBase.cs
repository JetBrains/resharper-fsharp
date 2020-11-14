using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public abstract class FSharpGeneratedElementPointerBase<TGenerated, TOrigin> : IDeclaredElementPointer<TGenerated>
    where TGenerated : class, IFSharpGeneratedFromOtherElement
    where TOrigin : class, IClrDeclaredElement
  {
    private IDeclaredElementPointer<IClrDeclaredElement> ElementPointer { get; }

    protected FSharpGeneratedElementPointerBase(TGenerated element) =>
      ElementPointer = element.OriginElement.CreateElementPointer();

    public TGenerated FindDeclaredElement() =>
      ElementPointer.FindDeclaredElement() is TOrigin origin
        ? CreateGenerated(origin)
        : null;

    [CanBeNull]
    public abstract TGenerated CreateGenerated(TOrigin fsElement);
  }
}
