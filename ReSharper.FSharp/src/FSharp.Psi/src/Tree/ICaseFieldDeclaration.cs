namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ICaseFieldDeclaration : IFSharpDeclaration
  {
    bool IsNameGenerated { get; }
  }
}
