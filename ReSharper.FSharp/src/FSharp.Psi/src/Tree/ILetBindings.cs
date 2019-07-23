namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ILetBindings
  {
    bool IsRecursive { get; }
    void SetIsRecursive(bool value);
  }
}
