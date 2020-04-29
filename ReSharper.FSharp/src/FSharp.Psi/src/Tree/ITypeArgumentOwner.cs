namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface ITypeArgumentOwner
  {
    ITypeArgumentList TypeArgumentList { get; }
  }
}
