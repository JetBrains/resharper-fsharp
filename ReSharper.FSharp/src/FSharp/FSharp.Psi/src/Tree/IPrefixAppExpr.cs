using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IPrefixAppExpr
  {
    bool IsHighPrecedence { get; }
    bool IsIndexerLike { get; }

    [CanBeNull] IFSharpExpression InvokedExpression { get; }
    [CanBeNull] IReferenceExpr InvokedReferenceExpression { get; }
    [CanBeNull] FSharpSymbolReference InvokedFunctionReference { get; }

    IList<IFSharpExpression> AppliedExpressions { get; }
  }
}
