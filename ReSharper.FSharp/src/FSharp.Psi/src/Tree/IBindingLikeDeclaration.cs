namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IBindingLikeDeclaration
  {
    bool IsMutable { get; }
    void SetIsMutable(bool value);
  }
}
