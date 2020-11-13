namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ITypeUsageOrUnionCaseDeclaration
  {
    bool CanBeUnionCase { get; }
  }
}
