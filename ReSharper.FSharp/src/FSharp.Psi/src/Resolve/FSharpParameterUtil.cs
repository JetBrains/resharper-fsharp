using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public static class FSharpParameterUtil
  {
    [CanBeNull]
    public static IDeclaredElement GetOwner([NotNull] this FSharpParameter fsParameter,
      [NotNull] FSharpSymbolReference reference)
    {
      var referenceOwner = reference.GetElement();
      if (referenceOwner is IReferenceExpr referenceExpr)
      {
        var binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(referenceExpr);
        if (binaryAppExpr is not { ShortName: "=" })
          return null;

        var innerExpr = (IFSharpExpression)TupleExprNavigator.GetByExpression(binaryAppExpr) ?? binaryAppExpr;
        var parenExpr = ParenOrBeginEndExprNavigator.GetByInnerExpression(innerExpr);

        if (!(PrefixAppExprNavigator.GetByArgumentExpression(parenExpr)?.FunctionExpression is IReferenceExpr expr))
          return null;

        var fcsSymbol = expr.Reference.GetFcsSymbol();
        switch (fcsSymbol)
        {
          case FSharpUnionCase unionCase:
            return GetFieldDeclaredElement(reference, unionCase, referenceOwner);

          case FSharpMemberOrFunctionOrValue mfv:
            // todo: fix member param declarations
            return mfv.GetDeclaredElement(referenceOwner.GetPsiModule(), referenceOwner) is IFunction functionElement
              ? functionElement.Parameters.FirstOrDefault(p => p.ShortName == reference.GetName())
              : null;
        }
      }

      if (referenceOwner is IExpressionReferenceName referenceName)
      {
        var fieldPat = FieldPatNavigator.GetByReferenceName(referenceName);
        var parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(fieldPat);
        if (parametersOwnerPat == null)
          return null;

        return parametersOwnerPat.ReferenceName.Reference.GetFcsSymbol() is FSharpUnionCase unionCase
          ? GetFieldDeclaredElement(reference, unionCase, referenceOwner)
          : null;
      }

      return null;
    }

    [CanBeNull]
    private static IDeclaredElement GetFieldDeclaredElement([NotNull] IReference reference,
      [NotNull] FSharpUnionCase unionCase, [NotNull] IFSharpReferenceOwner referenceOwner)
    {
      var field = unionCase.Fields.FirstOrDefault(f => f.Name == reference.GetName());
      return field?.GetDeclaredElement(referenceOwner.GetPsiModule(), referenceOwner);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IReadOnlyList<IReadOnlyList<(string name, ITreeNode node)>> GetParametersGroupNames(ITreeNode node) =>
      (node switch
      {
        IBinding binding => binding.Expression is ILambdaExpr lambda
          ? binding.ParameterPatterns.Select(GetParameterNames).Union(GetLambdaArgs(lambda))
          : binding.ParameterPatterns.Select(GetParameterNames),

        IBindingSignature bindingSignature => GetParameterNames(bindingSignature.ReturnTypeInfo.ReturnType),
        IMemberDeclaration member => member.ParameterPatterns.Select(GetParameterNames),
        // TODO: https://github.com/dotnet/fsharp/issues/13684
        //.Union(member.AccessorDeclarations.Select(t => t.ParameterPatterns.SelectMany(GetParameterNames))),
        IConstructorSignature constructorSignature => GetParameterNames(constructorSignature.ReturnTypeInfo.ReturnType),

        IConstructorDeclaration constructorDeclaration =>
          new[] { GetParameterNames(constructorDeclaration.ParameterPatterns) },

        IAbstractMemberDeclaration abstractMember => GetParameterNames(abstractMember.ReturnTypeInfo.ReturnType),

        IMemberSignature memberSignature => GetParameterNames(((IMemberSignatureOrDeclaration)memberSignature)
          .ReturnTypeInfo.ReturnType),

        IUnionCaseDeclaration { TypeUsage: { } typeUsage } => GetParameterNames(typeUsage),
        IUnionCaseDeclaration ucDecl => new[] { ucDecl.Fields.Select(t => t.SourceName) },

        IFSharpTypeDeclaration { TypeRepresentation: IDelegateRepresentation repr } =>
          GetParameterNames(repr.TypeUsage),

        IFSharpTypeDeclaration { PrimaryConstructorDeclaration: { } constructor } =>
          GetParametersGroupNames(constructor),

        _ => EmptyList<IEnumerable<(string, ITreeNode)>>.Enumerable
      })
      .Select(t => t.ToIReadOnlyList())
      .ToIReadOnlyList();

    private static bool IsSimplePattern(IFSharpPattern pattern, bool isTopLevel) => pattern.IgnoreInnerParens() switch
    {
      ILocalReferencePat or IAttribPat or ITypedPat or IWildPat => true,
      IUnitPat => isTopLevel,
      ITuplePat tuplePat => isTopLevel && tuplePat.PatternsEnumerable.All(t => IsSimplePattern(t, false)),
      _ => false
    };

    private static IEnumerable<IEnumerable<(string, ITreeNode)>> GetLambdaArgs(ILambdaExpr expr)
    {
      var lambdaParams = expr.Patterns;
      var parameters = lambdaParams.Select(GetParameterNames);
      if (expr.Expression is ILambdaExpr innerLambda && lambdaParams.All(pattern => IsSimplePattern(pattern, true)))
        parameters = parameters.Union(GetLambdaArgs(innerLambda));
      return parameters;
    }

    public static IEnumerable<(string, ITreeNode)> GetParameterNames(this IFSharpPattern pattern)
    {
      IEnumerable<(string, ITreeNode)> GetParameterNamesInternal(IFSharpPattern pat, bool isTopLevelParameter)
      {
        pat = pat.IgnoreInnerParens(true);
        return pat switch
        {
          IParenPat { Pattern: { } innerPat } => GetParameterNamesInternal(innerPat, false),
          ILocalReferencePat local => new[] { local.SourceName },
          IOptionalValPat opt => GetParameterNamesInternal(opt.Pattern, isTopLevelParameter),
          ITypedPat typed => GetParameterNamesInternal(typed.Pattern, false),
          IAttribPat attrib => GetParameterNamesInternal(attrib.Pattern, false),
          IAsPat asPat => GetParameterNamesInternal(asPat.RightPattern, false),
          ITuplePat tuplePat when isTopLevelParameter =>
            tuplePat.PatternsEnumerable.SelectMany(t => GetParameterNamesInternal(t, false)),
          var x => new[] { (SharedImplUtil.MISSING_DECLARATION_NAME, (ITreeNode)x) }
        };
      }

      return GetParameterNamesInternal(pattern, true);
    }

    public static IEnumerable<IEnumerable<(string, ITreeNode)>> GetParameterNames(this ITypeUsage pattern) =>
      pattern switch
      {
        IParenTypeUsage parenUsage => GetParameterNames(parenUsage.InnerTypeUsage),
        IConstrainedTypeUsage constrained => GetParameterNames(constrained.TypeUsage),
        IParameterSignatureTypeUsage local =>
          new[]
          {
            new[]
            {
              local.Identifier is { } identifier
                ? (identifier.Name, (ITreeNode)local)
                : (SharedImplUtil.MISSING_DECLARATION_NAME, null)
            }
          },
        IFunctionTypeUsage funPat =>
          GetParameterNames(funPat.ArgumentTypeUsage).Union(GetParameterNames(funPat.ReturnTypeUsage)),
        ITupleTypeUsage tuplePat => tuplePat.Items.SelectMany(GetParameterNames),
        _ => EmptyList<IEnumerable<(string, ITreeNode)>>.Enumerable
      };

    public static IReadOnlyList<IReadOnlyList<(string, ITreeNode)>> GetParametersGroups(this IBinding binding)
    {
      var parameters = binding.ParameterPatterns.Select(GetParameterNames);
      var bodyExpr = binding.Expression;

      while (bodyExpr.IgnoreInnerParens() is ILambdaExpr lambdaExpr)
      {
        parameters = parameters.Union(lambdaExpr.Patterns.Select(GetParameterNames));
        bodyExpr = lambdaExpr.Expression;
      }

      return parameters.Select(t => t.ToIReadOnlyList()).ToIReadOnlyList();
    }
  }
}
