using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class InfixAppExpr
  {
    public IReference InvokedFunctionReference => null;
    public IEnumerable<IExpression> Arguments => null;
  }
}
