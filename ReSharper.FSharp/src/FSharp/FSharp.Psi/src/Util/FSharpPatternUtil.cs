using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpPatternUtil
  {
    [ItemNotNull]
    public static IEnumerable<IFSharpPattern> GetPartialDeclarations([NotNull] this IFSharpPattern fsPattern)
    {
      if (!(fsPattern is IReferencePat refPat))
        return [fsPattern];

      var canBePartial = false;

      while (fsPattern.Parent is IFSharpPattern parent)
      {
        fsPattern = parent;

        if (parent is IOrPat || parent is IAndsPat)
          canBePartial = true;
      }

      if (!canBePartial)
        return [refPat];

      return fsPattern.NestedPatterns.Where(pattern =>
        pattern is IReferencePat nestedRefPat && nestedRefPat.SourceName == refPat.SourceName && pattern.IsDeclaration);
    }

    [CanBeNull]
    public static IBindingLikeDeclaration GetBinding([CanBeNull] this IFSharpPattern pat, bool allowFromParameters,
      out bool isFromParameter)
    {
      isFromParameter = false;

      if (pat == null)
        return null;

      var node = pat.Parent;
      while (node != null)
      {
        switch (node)
        {
          case IFSharpPattern:
            node = node.Parent;
            break;
          case IParametersPatternDeclaration when allowFromParameters:
            node = node.Parent;
            isFromParameter = true;
            break;
          case IBindingLikeDeclaration binding:
            return binding;
          default:
            return null;
        }
      }

      return null;
    }

    [CanBeNull]
    public static IBindingLikeDeclaration GetBindingFromHeadPattern([CanBeNull] this IFSharpPattern pat) =>
      GetBinding(pat, false, out _);

    [NotNull]
    public static ConstantValue GetConstantValue([CanBeNull] this IReferencePat pat)
    {
      var fcsSymbol = pat?.Reference?.GetFcsSymbol();
      if (fcsSymbol is FSharpMemberOrFunctionOrValue { LiteralValue: { Value: { } mfvValue } } mfv)
        return ConstantValue.Create(mfvValue, mfv.FullType.MapType(pat));

      if (fcsSymbol is FSharpField { LiteralValue.Value: { } fieldValue } field)
        return ConstantValue.Create(fieldValue, field.FieldType.MapType(pat));

      return ConstantValue.NOT_COMPILE_TIME_CONSTANT;
    }

    public static IFSharpParameter TryGetDeclaredFSharpParameter(this IFSharpPattern pat) =>
      TryGetFSharpParameterIndex(pat) is (IDeclaration
      {
        DeclaredElement: IFSharpParameterOwner parameterOwner
      }, var index)
        ? parameterOwner.GetParameter(index)
        : null;

    public static (IFSharpParameterOwnerDeclaration, FSharpParameterIndex)? TryGetFSharpParameterIndex(
      this IFSharpPattern fsPat)
    {
      (IFSharpPattern pat, int? index) GetContainingParameterGroupPattern()
      {
        if (TuplePatNavigator.GetByPattern(fsPat) is { } tuplePat)
        {
          var patternIndex = tuplePat.Patterns.IndexOf(fsPat);
          if (ParenPatNavigator.GetByPattern(tuplePat) is { } parenPat)
            return (parenPat, patternIndex);
        }

        if (ParenPatNavigator.GetByPattern(fsPat) is { } parentPat)
          return (parentPat, null);

        return (fsPat, null);
      }

      var (groupPat, paramIndex) = GetContainingParameterGroupPattern();

      if (GetParameterOwnerDeclaration(groupPat) is var (paramOwnerDecl, groupIndex))
        return (paramOwnerDecl, new FSharpParameterIndex(groupIndex, paramIndex));

      return null;
    }

    [CanBeNull]
    public static (IFSharpParameterOwnerDeclaration paramOwnerDecl, int groupIndex)? GetParameterOwnerDeclaration(IFSharpPattern groupPat)
    {
      if (ParameterOwnerMemberDeclarationNavigator.GetByParameterPattern(groupPat) is { } parameterOwnerMemberDecl)
        if (parameterOwnerMemberDecl is IFSharpParameterOwnerDeclaration paramOwnerDecl)
          return (paramOwnerDecl, parameterOwnerMemberDecl.ParameterPatterns.IndexOf(groupPat));

      if (LambdaExprNavigator.GetByPattern(groupPat) is { } lambdaExpr)
      {
        var lambdaParamGroupIndex = lambdaExpr.Patterns.IndexOf(groupPat);
        var containingDeclParamDeclsCount = 0;
        while (LambdaExprNavigator.GetByExpression(lambdaExpr.IgnoreParentParens()) is { } containingLambdaExpr)
        {
          containingDeclParamDeclsCount += containingLambdaExpr.PatternsEnumerable.Count();
          lambdaExpr = containingLambdaExpr;
        }

        if (ParameterOwnerMemberDeclarationNavigator.GetByExpression(lambdaExpr.IgnoreParentParens()) is { } pd)
          if (pd is IFSharpParameterOwnerDeclaration paramOwnerDecl)
            return (paramOwnerDecl, containingDeclParamDeclsCount + pd.ParameterPatterns.Count + lambdaParamGroupIndex);
      }

      return null;
    }


    public static IFSharpParameterDeclaration GetParameterDeclaration(
      this IList<IFSharpPattern> parameterPatternGroups, FSharpParameterIndex index)
    {
      if (parameterPatternGroups.ElementAtOrDefault(index.GroupIndex) is not { } groupPattern)
        return null;

      if (index.ParameterIndex is not { } paramIndex)
        return groupPattern;

      if (groupPattern is not IParenPat parenPat)
        return null;

      return parenPat.Pattern is ITuplePat tuplePat
        ? tuplePat.PatternsEnumerable.ElementAtOrDefault(paramIndex)
        : null;

    }

    private static int GetFcsGroupParameterCount([NotNull] IList<FSharpParameter> fcsParameterGroup) =>
      fcsParameterGroup.SingleItem() is { Type: { IsTupleType: true } fcsType }
        ? fcsType.GenericArguments.Count
        : fcsParameterGroup.Count;

    public static void SetParameterFcsType(this IList<IFSharpPattern> parameterPatternGroups,
      [NotNull] IFSharpParameterOwnerDeclaration paramOwnerDecl, FSharpParameterIndex index, FSharpType fcsType)
    {
      var typeAnnotationUtil = paramOwnerDecl.GetFSharpTypeAnnotationUtil();

      if (parameterPatternGroups.GetParameterDeclaration(index) is IReferencePat refPat)
      {
        var declPat = refPat.TryGetContainingParameterDeclarationPattern();
        typeAnnotationUtil.SetPatternFcsType(declPat, fcsType);
        return;
      }

      if (parameterPatternGroups.ElementAtOrDefault(index.GroupIndex) is not { } groupPattern)
        return;

      if (index.ParameterIndex is { } parameterIndex)
      {
        if (groupPattern.IgnoreInnerParens() is ITypedPat { TypeUsage: ITupleTypeUsage tupleTypeUsage })
        {
          if (tupleTypeUsage.Items.ElementAtOrDefault(parameterIndex) is { } paramTypeUsage)
            typeAnnotationUtil.ReplaceWithFcsType(paramTypeUsage, fcsType);
        }
        else
        {
          // todo: union case
          if (paramOwnerDecl.GetFcsSymbol() is not FSharpMemberOrFunctionOrValue mfv)
            return;

          var fcsParamGroup = mfv.CurriedParameterGroups.ElementAtOrDefault(index.GroupIndex);
          if (fcsParamGroup == null)
            return;

          var fcsGroupParameterCount = GetFcsGroupParameterCount(fcsParamGroup);
          if (fcsGroupParameterCount == 1)
          {
            typeAnnotationUtil.SetPatternFcsType(groupPattern, fcsType);
            return;
          }

          var factory = paramOwnerDecl.CreateElementFactory();
          var tupleTypeString = Enumerable.Repeat("_", fcsGroupParameterCount).Join(" * ");
          var newTypeUsage = factory.CreateTypeUsage(tupleTypeString, TypeUsageContext.TopLevel);
          var typeUsage = (ITupleTypeUsage)typeAnnotationUtil.SetPatternTypeUsage(groupPattern, newTypeUsage).TypeUsage;
          if (typeUsage.Items.ElementAtOrDefault(parameterIndex) is { } paramTypeUsage)
            typeAnnotationUtil.ReplaceWithFcsType(paramTypeUsage, fcsType);
        }
      }
      else
      {
        typeAnnotationUtil.SetPatternFcsType(groupPattern, fcsType);
      }
    }

    public static bool IsFSharpParameterDeclaration(IFSharpPattern fsPat) =>
      TryGetFSharpParameterIndex(fsPat) != null;

    [CanBeNull] public static IFSharpParameter TryGetDeclaredFSharpParameter([NotNull] this IReferencePat refPat) =>
      refPat.TryGetContainingParameterDeclarationPattern().TryGetDeclaredFSharpParameter();

    public static IFSharpPattern TryGetContainingParameterDeclarationPattern(this IReferencePat refPat)
    {
      IFSharpPattern TryUnwrap(IFSharpPattern pat)
      {
        if (AsPatNavigator.GetByRightPattern(pat) is { } asPat) return asPat;
        if (AttribPatNavigator.GetByPattern(pat) is { } attribPat) return attribPat;
        if (OptionalValPatNavigator.GetByPattern(pat) is { } optionalValPat) return optionalValPat;
        if (ParenPatNavigator.GetByPattern(pat) is { } parenPat) return parenPat;
        if (TypedPatNavigator.GetByPattern(pat) is { } typedPat) return typedPat;

        return null;
      }

      var pat = (IFSharpPattern)refPat;
      while (TryUnwrap(pat) is { } containingPat)
        pat = containingPat;

      return IsFSharpParameterDeclaration(pat) ? pat : null;
    }

    public static IReferencePat TryGetNameIdentifierOwner([NotNull] this IFSharpPattern fsPat)
    {
      IFSharpPattern TryUnwrap(IFSharpPattern pat) =>
        pat switch
        {
          IAttribPat attribPat => attribPat.Pattern,
          IAsPat asPat => asPat,
          IOptionalValPat optionalValPat => optionalValPat.Pattern,
          IParenPat parenPat => parenPat.Pattern,
          ITypedPat typedPat => typedPat.Pattern,
          _ => null
        };

      var pat = fsPat;
      while (TryUnwrap(pat) is { } innerPat)
        pat = innerPat;

      return pat as IReferencePat;
    }
  }
}
