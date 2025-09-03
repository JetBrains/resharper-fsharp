using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface ISecondaryDeclaredElement
{
  [CanBeNull] IClrDeclaredElement OriginElement { get; }
}
