namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ITypeAbbreviationDeclaration
  {
    bool CanBeUnionCase { get; }
  }
}
