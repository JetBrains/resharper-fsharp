namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface INestedModuleDeclaration
  {
    IFSharpTypeDeclaration GetAssociatedTypeDeclaration(out string sourceName);
  }
}