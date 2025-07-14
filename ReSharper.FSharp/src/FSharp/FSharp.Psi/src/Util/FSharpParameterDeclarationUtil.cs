using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util;

public static class FSharpParameterDeclarationUtil
{
  [NotNull]
  public static IList<IFSharpParameterDeclaration> GetParameterDeclarations([CanBeNull] IFSharpPattern fsPattern)
  {
    var paramGroup = new List<IFSharpParameterDeclaration>();
    if (fsPattern is IParenPat parenPat)
    {
      var innerPat = parenPat.Pattern;
      if (innerPat is ITuplePat tuplePat)
        paramGroup.AddRange(tuplePat.PatternsEnumerable);
      else
        paramGroup.Add(innerPat);
    }
    else
    {
      paramGroup.Add(fsPattern);
    }

    return paramGroup;
  }

  [NotNull]
  public static List<IList<IFSharpParameterDeclaration>> GetParameterDeclarations(
    this TreeNodeCollection<IFSharpPattern> paramPatternDecls)
  {
    var result = new List<IList<IFSharpParameterDeclaration>>(paramPatternDecls.Count);
    result.AddRange(paramPatternDecls.Select(GetParameterDeclarations));
    return result;
  }

  public static IList<IList<IFSharpParameterDeclaration>> GetBindingParameterDeclarations(this IBinding binding)
  {
    var result = binding.ParameterPatterns.GetParameterDeclarations();
    result.AddRange(GetTopLevelParameterPatterns(binding.Expression).Select(GetParameterDeclarations));
    return result;
  }

  public static IList<IFSharpPattern> GetBindingParameterPatterns(this IBinding binding)
  {
    var result = new List<IFSharpPattern>(binding.ParameterPatterns);
    result.AddRange(GetTopLevelParameterPatterns(binding.Expression));
    return result;
  }


  private static IEnumerable<IFSharpPattern> GetTopLevelParameterPatterns([CanBeNull] IFSharpExpression expr)
  {
    while (expr.IgnoreInnerParens() is ILambdaExpr lambdaExpr)
    {
      foreach (var pattern in lambdaExpr.Patterns)
        yield return pattern;

      expr = lambdaExpr.Expression;
    }
  }

  [CanBeNull]
  [SuppressMessage("ReSharper", "VariableHidesOuterVariable")]
  public static IFSharpParameterDeclaration GetParameterDeclaration([CanBeNull] this ITypeUsage typeUsage,
    FSharpParameterIndex index)
  {
    ITypeUsage GetParameterGroupTypeUsage(ITypeUsage typeUsage, int index)
    {
      for (var i = 0; i < index; i++)
      {
        if (typeUsage is not IFunctionTypeUsage functionTypeUsage)
          return null;

        typeUsage = functionTypeUsage.ReturnTypeUsage;
      }

      return typeUsage is IFunctionTypeUsage functionTypeUsage1
        ? functionTypeUsage1.ArgumentTypeUsage
        : null;
    }

    IFSharpParameterDeclaration GetParameterSigTypeUsage(ITypeUsage groupTypeUsage, int? paramIndex)
    {
      if (paramIndex is { } index)
        return groupTypeUsage is ITupleTypeUsage tupleTypeUsage
          ? tupleTypeUsage.ItemsEnumerable.ElementAtOrDefault(index) as IFSharpParameterDeclaration
          : null;

      return groupTypeUsage as IFSharpParameterDeclaration;
    }

    var groupTypeUsage = GetParameterGroupTypeUsage(typeUsage, index.GroupIndex);
    return GetParameterSigTypeUsage(groupTypeUsage, index.ParameterIndex);
  }


  [NotNull]
  public static IList<IList<IFSharpParameterDeclaration>> GetParameterDeclarations(
    [CanBeNull] this ITypeUsage typeUsage)
  {
    // ReSharper disable once VariableHidesOuterVariable
    IList<IFSharpParameterDeclaration> GetParameterDeclarationsInGroup(ITypeUsage typeUsage)
    {
      var group = new List<IFSharpParameterDeclaration>();
      switch (typeUsage)
      {
        case ITupleTypeUsage tupleTypeUsage:
        {
          foreach (var itemTypeUsage in tupleTypeUsage.ItemsEnumerable)
            if (itemTypeUsage is IParameterSignatureTypeUsage paramSigTypeUsage)
              group.Add(paramSigTypeUsage);
          break;
        }
        case IParameterSignatureTypeUsage paramSigTypeUsage:
          group.Add(paramSigTypeUsage);
          break;
      }

      return group;
    }

    var result = new List<IList<IFSharpParameterDeclaration>>();
    while (typeUsage is IFunctionTypeUsage functionTypeUsage)
    {
      result.Add(GetParameterDeclarationsInGroup(functionTypeUsage.ArgumentTypeUsage));
      typeUsage = functionTypeUsage.ReturnTypeUsage;
    }

    return result;
  }
}
