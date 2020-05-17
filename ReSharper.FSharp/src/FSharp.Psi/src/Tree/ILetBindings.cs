namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ILetBindings
  {
    bool IsUse { get; }
    bool IsRecursive { get; }
    void SetIsRecursive(bool value);
  }
}
