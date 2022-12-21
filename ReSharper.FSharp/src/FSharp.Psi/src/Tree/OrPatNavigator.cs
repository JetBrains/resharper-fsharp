namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class OrPatNavigator
  {
    [JetBrains.Annotations.Pure]
    [JetBrains.Annotations.CanBeNull]
    [JetBrains.Annotations.ContractAnnotation("null <= null")]
    public static IOrPat GetByPattern(IFSharpPattern param) =>
      GetByPattern1(param) ?? GetByPattern2(param);
  }
}
