using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class FSharpArgumentsOwnerUtil
  {
    public static IList<IArgument> CalculateParameterArguments(this IFSharpReferenceOwner referenceOwner,
      IEnumerable<IFSharpExpression> appliedExpressions)
    {
      var fcsSymbol = referenceOwner.Reference.GetFcsSymbol();

      if (fcsSymbol is FSharpUnionCase unionCase)
      {
        if (!unionCase.HasFields) return EmptyArray<IArgument>.Instance;

        if (appliedExpressions.FirstOrDefault() is not { } appliedArg)
          return EmptyArray<IArgument>.Instance;

        appliedArg = appliedArg.IgnoreInnerParens();

        return appliedArg is not ITupleExpr tupleExpr
          ? new List<IArgument>(1) { appliedArg as IArgument }
          : tupleExpr.Expressions.Take(unionCase.Fields.Count).ToList(t => t as IArgument);
      }

      if (fcsSymbol is not FSharpMemberOrFunctionOrValue mfv)
        return EmptyList<IArgument>.Instance;

      var paramGroups = mfv.CurriedParameterGroups;
      var isVoidReturn = paramGroups.Count == 1 && paramGroups[0].Count == 1 && paramGroups[0][0].Type.IsUnit();

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
