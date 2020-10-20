namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IType2ExtensionDeclaration
  {
    bool IsTypePartDeclaration { get; }
    bool IsTypeExtensionAllowed { get; }
  }
}
