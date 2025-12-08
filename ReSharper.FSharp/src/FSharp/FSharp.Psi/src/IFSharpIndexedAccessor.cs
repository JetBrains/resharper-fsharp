using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpIndexedAccessor : IMethod, IFSharpParameterOwner, ISecondaryDeclaredElement
  {
    AccessorKind Kind { get; }
  }
}
