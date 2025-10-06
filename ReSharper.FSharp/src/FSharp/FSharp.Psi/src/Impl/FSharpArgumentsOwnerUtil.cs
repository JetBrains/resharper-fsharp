using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
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

        return appliedArg is ITupleExpr tupleExpr
          ? tupleExpr.Expressions.Take(unionCase.Fields.Count).ToList(t => t as IArgument)
          : new List<IArgument> { appliedArg as IArgument };
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
          var canHaveNamedArgs = mfv.IsMember;

          // e.g. F# extension methods with 0 parameters
          if (paramGroup.Count == 0)
            return EmptyList<IArgument>.Instance;

          if (paramGroup.Count == 1)
          {
            // RIDER-125426
            // argExpr could be a tuple with a corresponding single param group parameter
            // if it additionally contains property initializers
            var hasNamedArgInTuple =
              canHaveNamedArgs &&
              argExpr is ITupleExpr tupleExpr &&
              ParenExprNavigator.GetByInnerExpression(ParenExprNavigator.GetByInnerExpression(tupleExpr)) == null &&
              tupleExpr.ExpressionsEnumerable.Any(arg =>
                arg is IBinaryAppExpr binaryAppExpr && FSharpArgumentsUtil.HasNamedArgStructure(binaryAppExpr));

            if (!hasNamedArgInTuple) return [argExpr as IArgument];
          }

          var tupleExprs =
            argExpr switch
            {
              ITupleExpr tupleExpr => (IList<IFSharpExpression>)tupleExpr.Expressions,
              // if the second parameter is optional (and hence all subsequent ones)
              // then F# allows one argument to be passed
              //
              // member M(a, ?b, ...)
              // >> M(1)
              _ when paramGroup[1].IsOptionalArg => new[] { argExpr }, //RIDER-96778
              _ => EmptyList<IFSharpExpression>.Instance
            };

          IList<IFSharpExpression> unnamedArgs = new List<IFSharpExpression>();
          var namedLikeArgs = new Dictionary<string, IArgument>();

          if (canHaveNamedArgs)
            foreach (var expr in tupleExprs)
            {
              if (FSharpArgumentsUtil.IsTopLevelArg(expr) &&
                  FSharpArgumentsUtil.TryGetNamedArgRefExpr(expr) is { ShortName: { } name })
                namedLikeArgs.TryAdd(name, expr as IArgument);

              else unnamedArgs.Add(expr);
            }
          else unnamedArgs = tupleExprs;

          return paramGroup.Select((x, i) =>
          {
            if (i < unnamedArgs.Count) return unnamedArgs[i] as IArgument;
            if (x.Name?.Value is { } paramName) return namedLikeArgs.GetValueSafe(paramName);
            return null;
          });
        }).ToList();
    }

    public static FSharpParameterIndex? TryGetFSharpParameterIndex([NotNull] IFSharpTypeOwnerNode node)
    {
      if (node is IFSharpExpression expr)
      {
        // todo: named args
        // todo: return property setters

        var tupleItemExpr = expr.IgnoreParentParens();
        var tupleExpr = TupleExprNavigator.GetByExpression(tupleItemExpr);
        var parameterIndex = tupleExpr?.Expressions.IndexOf(tupleItemExpr);

        var argGroupExpr = tupleExpr.IgnoreParentParens() ?? tupleItemExpr;
        var argOwner = FSharpArgumentOwnerNavigator.GetByArgumentExpression(argGroupExpr);
        if (argOwner == null)
          return null;

        if (argOwner is IAttribute or INewExpr)
          return new FSharpParameterIndex(0, parameterIndex);

        var paramGroupIndex = 0;
        while (argOwner is IPrefixAppExpr prefixAppExpr)
        {
          if (prefixAppExpr.FunctionExpression is IPrefixAppExpr funPrefixAppExpr)
          {
            argOwner = funPrefixAppExpr;
            paramGroupIndex++;
          }
          else
            break;
        }

        return new FSharpParameterIndex(paramGroupIndex, parameterIndex);
      }

      if (node is IFSharpPattern pat)
      {
      }

      return null;
    }
  }
}
