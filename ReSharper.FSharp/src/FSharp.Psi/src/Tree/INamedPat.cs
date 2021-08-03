namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface INamedPat : IFSharpDeclaration
  {
    bool IsLocal { get; }
  }
}
