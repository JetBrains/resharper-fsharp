using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IModule : IFSharpTypeElement
  {
    bool IsAnonymous { get; }
    bool IsAutoOpen { get; }

    [CanBeNull] ITypeElement AssociatedTypeElement { get; }
  }
}
