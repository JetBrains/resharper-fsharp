using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IModule : ITypeElement
  {
    bool IsAnonymous { get; }
  }
}