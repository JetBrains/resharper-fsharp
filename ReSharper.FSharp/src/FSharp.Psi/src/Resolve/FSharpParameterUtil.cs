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

        var innerExpr = (IFSharpExpression) TupleExprNavigator.GetByExpression(binaryAppExpr) ?? binaryAppExpr;
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

    public static IReadOnlyList<IReadOnlyList<string>> GetParametersGroupNames(ITreeNode node) =>
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

        IUnionCaseDeclaration ucDecl => new[] { ucDecl.Fields.Select(t => t.SourceName) },

        IFSharpTypeDeclaration { TypeRepresentation: IDelegateRepresentation repr } =>
          GetParameterNames(repr.TypeUsage),

        _ => EmptyList<string[]>.Enumerable
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

    private static IEnumerable<IEnumerable<string>> GetLambdaArgs(ILambdaExpr expr)
    {
      var lambdaParams = expr.Patterns;
      var parameters = lambdaParams.Select(GetParameterNames);
      if (expr.Expression is ILambdaExpr innerLambda && lambdaParams.All(pattern => IsSimplePattern(pattern, true)))
        parameters = parameters.Union(GetLambdaArgs(innerLambda));
      return parameters;
    }

    public static IEnumerable<string> GetParameterNames(this IFSharpPattern pattern)
    {
      IEnumerable<string> GetParameterNamesInternal(IFSharpPattern pat, bool isTopLevelParameter)
      {
        pat = pat.IgnoreInnerParens();
        return pat switch
        {
          ILocalReferencePat local => new[] { local.SourceName },
          IOptionalValPat opt => GetParameterNamesInternal(opt.Pattern, isTopLevelParameter),
          ITypedPat typed => GetParameterNamesInternal(typed.Pattern, false),
          IAttribPat attrib => GetParameterNamesInternal(attrib.Pattern, false),
          IAsPat asPat => GetParameterNamesInternal(asPat.RightPattern, false),
          ITuplePat tuplePat when isTopLevelParameter =>
            tuplePat.PatternsEnumerable.SelectMany(t => GetParameterNamesInternal(t, false)),
          _ => new[] { SharedImplUtil.MISSING_DECLARATION_NAME }
        };
      }

      return GetParameterNamesInternal(pattern, true);
    }

    public static IEnumerable<IEnumerable<string>> GetParameterNames(this ITypeUsage pattern) =>
      pattern switch
      {
        IParenTypeUsage parenUsage => GetParameterNames(parenUsage.InnerTypeUsage),
        IConstrainedTypeUsage constrained => GetParameterNames(constrained.TypeUsage),
        IParameterSignatureTypeUsage local =>
          new[] { new[] { local.Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME } },
        IFunctionTypeUsage funPat =>
          GetParameterNames(funPat.ArgumentTypeUsage).Union(GetParameterNames(funPat.ReturnTypeUsage)),
        ITupleTypeUsage tuplePat => tuplePat.Items.SelectMany(GetParameterNames),
        _ => EmptyList<IEnumerable<string>>.Enumerable
      };
  }
}
