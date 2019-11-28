using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IAppExpr
  {
    [CanBeNull] FSharpSymbolReference InvokedFunctionReference { get; }
    [NotNull] IList<IExpression> Arguments { get; }
  }
}
