using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NewExpr
  {
    public FSharpSymbolReference Reference { get; private set; }

    protected override void PreInit()
    {
      base.PreInit();
      Reference = new CtorReference(this);
    }

    public IList<IArgument> Arguments
    {
      get
      {
        // todo: this is same as Attribute.Arguments
        switch (ArgumentExpression.IgnoreInnerParens())
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

    public IFSharpIdentifier FSharpIdentifier => TypeName?.Identifier;

    public IFSharpReferenceOwner SetName(string name)
    {
      throw new System.NotImplementedException();
    }

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(Reference);

    public IList<IFSharpExpression> AppliedExpressions => new List<IFSharpExpression> {ArgumentExpression};
  }
}
