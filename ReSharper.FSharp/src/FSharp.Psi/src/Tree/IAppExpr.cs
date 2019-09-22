using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IAppExpr
  {
    [CanBeNull] IReference InvokedFunctionReference { get; }
    [CanBeNull] IEnumerable<IExpression> Arguments { get; }
  }
}
