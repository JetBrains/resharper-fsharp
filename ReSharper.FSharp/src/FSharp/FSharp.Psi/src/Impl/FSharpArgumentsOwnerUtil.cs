using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class FSharpArgumentsOwnerUtil
  {
    [CanBeNull]
    public static IFSharpMethodInvocationUtil GetInvocationUtil([NotNull] this ITreeNode node) =>
      node.GetSolution().GetComponent<LanguageManager>()
        .TryGetService<IFSharpMethodInvocationUtil>(FSharpLanguage.Instance);

    public static IList<IArgument> CalculateParameterArguments([NotNull] this IFSharpArgumentsOwner argsOwner) =>
      argsOwner.GetInvocationUtil()?.GetParameterArguments(argsOwner) ?? EmptyList<IArgument>.InstanceList;

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
          ? MatchGroupArguments(tupleExpr.Expressions, unionCase.Fields.Select(field => field.Name).ToList(),
            canHaveNamedArgs: true, canHaveSetters: false).ToList()
          : new List<IArgument> { appliedArg as IArgument };
      }

      if (fcsSymbol is not FSharpMemberOrFunctionOrValue mfv)
        return EmptyList<IArgument>.Instance;

      var paramGroups = mfv.CurriedParameterGroups;
      var isVoidReturn = paramGroups.Count == 1 && paramGroups[0].Count == 1 && paramGroups[0][0].Type.IsUnitType;

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
              tupleExpr.ExpressionsEnumerable.Any(FSharpArgumentsUtil.HasNamedArgStructure);

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

          return MatchGroupArguments(tupleExprs, paramGroup.Select(param => param.Name?.Value).ToList(),
            canHaveNamedArgs, canHaveSetters: canHaveNamedArgs);
        }).ToList();
    }

    private static IEnumerable<IArgument> MatchGroupArguments(IList<IFSharpExpression> argExprs,
      IList<string> parameterNames, bool canHaveNamedArgs, bool canHaveSetters)
    {
      var unnamedArgs = new List<IFSharpExpression>();
      var namedLikeArgs = new Dictionary<string, IArgument>();

      foreach (var expr in argExprs)
      {
        var name = canHaveNamedArgs && FSharpArgumentsUtil.IsTopLevelArg(expr)
          ? FSharpArgumentsUtil.TryGetNamedArgName(expr)
          : null;

        if (name != null && (canHaveSetters || parameterNames.Contains(name)))
          namedLikeArgs.TryAdd(name, expr as IArgument);
        else
          unnamedArgs.Add(expr);
      }

      return parameterNames.Select((parameterName, i) =>
      {
        if (i < unnamedArgs.Count) return unnamedArgs[i] as IArgument;
        if (parameterName != null) return namedLikeArgs.GetValueSafe(parameterName);
        return null;
      });
    }

    [CanBeNull]
    public static FSharpSymbolReference GetInvokedFunctionReference([NotNull] this IReferenceExpr referenceExpr,
      int appliedExpressionsCount)
    {
      var reference = referenceExpr.Reference;
      var fcsSymbol = reference.GetFcsSymbol();

      // todo: exceptions
      var isValid = fcsSymbol switch
      {
        FSharpMemberOrFunctionOrValue mfv => mfv.CurriedParameterGroups.Count >= appliedExpressionsCount,
        FSharpUnionCase unionCase => unionCase.HasFields && appliedExpressionsCount <= 1,
        FSharpEntity fcsEntity => fcsEntity.IsFSharpExceptionDeclaration,
        _ => false
      };

      return isValid ? reference : null;
    }

    public static FSharpParameterIndex? TryGetFSharpParameterIndex([NotNull] IFSharpPattern pat,
      out IFSharpArgumentsOwner argOwner)
    {
      argOwner = null;

      var argPat = GetArgGroupPat(out var parameterIndex, out var namedArg);
      if (ParametersOwnerPatNavigator.GetByParameter(argPat) is not { } parametersOwnerPat)
        return null;

      argOwner = parametersOwnerPat;
      return new FSharpParameterIndex(0, parameterIndex, namedArg);

      IFSharpPattern GetArgGroupPat(out int? parameterIndex, out string namedArg)
      {
        parameterIndex = null;
        namedArg = null;

        if (FieldPatNavigator.GetByPattern(pat) is { } fieldPat)
        {
          namedArg = fieldPat.ShortName;
          return NamedUnionCaseFieldsPatNavigator.GetByFieldPattern(fieldPat);
        }
        else
        {
          var tupleItemPat = pat.IgnoreParentParens();
          var tuplePat = TuplePatNavigator.GetByPattern(tupleItemPat);
          parameterIndex = tuplePat?.Patterns.IndexOf(tupleItemPat);

          return tuplePat.IgnoreParentParens() ?? tupleItemPat;
        }
      }
    }
  }
}
