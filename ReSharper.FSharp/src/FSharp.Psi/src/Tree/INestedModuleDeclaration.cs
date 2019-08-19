namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface INestedModuleDeclaration : IFSharpTypeElementDeclaration
  {
    IFSharpTypeDeclaration GetAssociatedTypeDeclaration(out string sourceName);
  }
}