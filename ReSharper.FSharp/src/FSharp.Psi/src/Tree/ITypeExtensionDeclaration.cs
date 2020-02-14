namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ITypeExtensionDeclaration : IFSharpReferenceOwner
  {
    bool IsTypePartDeclaration { get; }
    bool IsTypeExtensionAllowed { get; }
  }
}
