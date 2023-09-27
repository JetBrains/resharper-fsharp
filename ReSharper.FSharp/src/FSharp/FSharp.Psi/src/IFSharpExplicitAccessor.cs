using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpExplicitAccessor : IMethod, IFSharpMember, ISecondaryDeclaredElement
  {
    AccessorKind Kind { get; }
  }
}
