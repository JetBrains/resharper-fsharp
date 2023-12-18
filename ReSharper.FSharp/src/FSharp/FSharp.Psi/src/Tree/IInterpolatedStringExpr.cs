using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Injections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IInterpolatedStringExpr : IInjectionHostNode
  {
    bool IsTrivial();
    DocumentRange GetDollarSignRange();
    int DollarCount { get; }
  }
}
