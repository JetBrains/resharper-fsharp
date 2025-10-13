using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpFunctionalTypeField : IFSharpDeclaredElement, ITypeOwner
{
  int Index { get; }
}
