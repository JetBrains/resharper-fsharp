using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpExplicitAccessor : IMethod, IFSharpParameterOwner, ISecondaryDeclaredElement
  {
    AccessorKind Kind { get; }
  }
}
