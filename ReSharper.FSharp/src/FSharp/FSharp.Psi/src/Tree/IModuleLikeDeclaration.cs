namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IModuleLikeDeclaration : IFSharpDeclaration
  {
    string ClrName { get; }
  }
}
