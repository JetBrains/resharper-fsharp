namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ITypeExtensionDeclaration
  {
    bool IsTypePartDeclaration { get; }
    bool IsTypeExtensionAllowed { get; }
  }
}
