using System.Collections.Generic;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class PrefixAppExpr
  {
    public IReference InvokedFunctionReference
    {
      get
      {
        var argsCount = 0;
        var funExpr = (IAppExpr) this;
        while (funExpr.FunctionExpression.IgnoreInnerParens() is IAppExpr appExpr)
        {
          funExpr = appExpr;
          argsCount++;
        }

        if (!(funExpr.FunctionExpression.IgnoreInnerParens() is IReferenceExpr referenceExpr))
          return null;

        argsCount++;

        var reference = referenceExpr.Reference;
        var fsSymbol = reference.GetFSharpSymbol();

        // todo: union cases, exceptions
        if (!(fsSymbol is FSharpMemberOrFunctionOrValue mfv))
          return null;

        var paramGroups = mfv.CurriedParameterGroups;
        if (paramGroups.Count != argsCount)
          return null;

        return reference;
      }
    }

    public IEnumerable<IExpression> Arguments
    {
      get
      {
        var args = new List<IExpression>();
        var funExpr = (IAppExpr) this;
        while (funExpr.FunctionExpression.IgnoreInnerParens() is IAppExpr appExpr)
        {
          args.Add(funExpr.ArgumentExpression);
          funExpr = appExpr;
        }

        args.Add(funExpr.ArgumentExpression);
        args.Reverse();
        return args;
      }
    }
  }
}
