using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Injections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IInterpolatedStringExpr : IInjectionHostNode
  {
    public bool IsTrivial();
    public DocumentRange GetDollarSignRange();
  }
}
