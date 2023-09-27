namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IDeclaredModuleLikeDeclaration
  {
    bool IsRecursive { get; }
    void SetIsRecursive(bool value);
  }
}
