namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ILetBindingsDeclaration
  {
    bool IsInline { get; }
    void SetIsInline(bool value);
  }
}
