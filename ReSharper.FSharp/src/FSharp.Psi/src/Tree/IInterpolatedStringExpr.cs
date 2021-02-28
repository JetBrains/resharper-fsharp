using JetBrains.DocumentModel;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IInterpolatedStringExpr
  {
    public bool IsTrivial();
    public DocumentRange GetDollarSignRange();
  }
}
