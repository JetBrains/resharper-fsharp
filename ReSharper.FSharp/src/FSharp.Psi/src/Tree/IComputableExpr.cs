namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IComputableExpr
  {
    bool HasBangInBindingKeyword { get; }
  }
}
