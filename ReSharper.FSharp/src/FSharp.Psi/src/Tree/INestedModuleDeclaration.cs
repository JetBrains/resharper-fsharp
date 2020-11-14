namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface INestedModuleDeclaration
  {
    IFSharpTypeOldDeclaration GetAssociatedTypeDeclaration(out string sourceName);
  }
}