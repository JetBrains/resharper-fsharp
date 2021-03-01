using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class FSharpArgumentsOwnerUtil
  {
    public static IList<IArgument> CalculateParameterArguments(IFSharpReferenceOwner referenceOwner,
      IEnumerable<IFSharpExpression> appliedExpressions)
    {
      if (!(referenceOwner.Reference.GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv))
        return EmptyList<IArgument>.Instance;

      var paramGroups = mfv.CurriedParameterGroups;
      var isVoidReturn = paramGroups.Count == 1 && paramGroups[0].Count == 1 && paramGroups[0][0].Type.IsUnit;

      if (isVoidReturn)
        return EmptyArray<IArgument>.Instance;

      return paramGroups
        .Zip(appliedExpressions, (paramGroup, argExpr) => (paramGroup, argExpr.IgnoreInnerParens()))
        .SelectMany(pair =>
        {
          var (paramGroup, argExpr) = pair;

          // e.g. F# extension methods with 0 parameters
          if (paramGroup.Count == 0)
            return EmptyList<IArgument>.Instance;

          if (paramGroup.Count == 1)
            return new[] {argExpr as IArgument};

          var tupleExprs = argExpr is ITupleExpr tupleExpr
            ? (IReadOnlyList<IFSharpExpression>) tupleExpr.Expressions
            : EmptyList<IFSharpExpression>.Instance;

          return Enumerable.Range(0, paramGroup.Count)
            .Select(i => i < tupleExprs.Count ? tupleExprs[i] as IArgument : null);
        }).ToList();
    }
  }
}
