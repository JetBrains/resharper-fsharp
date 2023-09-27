namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface INestedModuleDeclaration
  {
    IFSharpTypeOrExtensionDeclaration GetAssociatedTypeDeclaration(out string sourceName);
  }
}