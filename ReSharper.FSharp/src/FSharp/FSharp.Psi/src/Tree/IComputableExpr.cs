namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  /// <summary>
  /// Common interface for expression types that may have a `!` variant
  /// </summary>
  public partial interface IComputableExpr
  {
    /// <summary>
    /// Indicates whether the expression has the `!` in the keyword.
    /// For example `let!` or `match!`.
    /// </summary>
    bool IsComputed { get; }
  }
}
