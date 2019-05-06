namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IModule : IFSharpTypeElement
  {
    bool IsAnonymous { get; }
  }
}
