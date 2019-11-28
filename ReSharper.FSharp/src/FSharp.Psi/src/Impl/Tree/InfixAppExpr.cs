using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class InfixAppExpr
  {
    public FSharpSymbolReference InvokedFunctionReference => null;
    public IList<IExpression> Arguments => EmptyList<IExpression>.Instance;
  }
}
