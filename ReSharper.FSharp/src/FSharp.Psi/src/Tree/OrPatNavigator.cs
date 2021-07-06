namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class OrPatNavigator
  {
    [Annotations.Pure]
    [Annotations.CanBeNull]
    [Annotations.ContractAnnotation("null <= null")]
    public static IOrPat GetByPattern(IFSharpPattern param) =>
      GetByPattern1(param) ?? GetByPattern2(param);
  }
}
