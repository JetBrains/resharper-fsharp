namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IComputableExpr
  {
    /// <summary>
    ///  Indicates whether the expression has the `!` in the keyword.
    /// For example `let!` or `use!`.
    /// </summary>
    bool IsComputed { get; }
  }
}
