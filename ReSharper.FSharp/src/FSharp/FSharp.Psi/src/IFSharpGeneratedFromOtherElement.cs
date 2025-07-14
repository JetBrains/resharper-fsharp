using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpGeneratedFromOtherElement : IFSharpGeneratedElement, ISecondaryDeclaredElement
{
  IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer();
}
