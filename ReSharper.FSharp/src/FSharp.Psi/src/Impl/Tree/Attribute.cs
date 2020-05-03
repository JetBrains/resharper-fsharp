using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class Attribute
  {
    protected override FSharpSymbolReference CreateReference() =>
      new CtorReference(this);

    public override IFSharpIdentifier FSharpIdentifier => ReferenceName?.Identifier;

    public IList<IArgument> Arguments
    {
      get
      {
        // todo: this is same as NewExpr.Arguments
        switch (ArgExpression?.Expression.IgnoreInnerParens())
        {
          case IUnitExpr _:
            return EmptyList<IArgument>.Instance;
          case ITupleExpr tupleExpr:
            return tupleExpr.Expressions.Select(arg => arg as IArgument).ToList();
          case IArgument argExpr:
            return new List<IArgument> { argExpr };
          default:
            return EmptyList<IArgument>.Instance;
        }
      }
    }

    public IList<IFSharpExpression> AppliedExpressions => new List<IFSharpExpression> {ArgExpression.Expression};
  }
}
