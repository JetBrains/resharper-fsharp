using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util;

public static class FSharpParameterUtil
{
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
      IUnionCaseDeclaration ucDecl => new[] { ucDecl.Fields.Select(t => (t.SourceName, (ITreeNode)t)) },

      IFSharpTypeDeclaration { TypeRepresentation: IDelegateRepresentation repr } =>
        GetParameterNames(repr.TypeUsage),

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
        ILocalReferencePat local => new[] { (local.SourceName, (ITreeNode)local) },
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
              ? (identifier.Name, (ITreeNode)identifier)
              : (SharedImplUtil.MISSING_DECLARATION_NAME, null)
          }
        },
      IFunctionTypeUsage funPat =>
        GetParameterNames(funPat.ArgumentTypeUsage).Union(GetParameterNames(funPat.ReturnTypeUsage)),
      ITupleTypeUsage tuplePat => tuplePat.Items.SelectMany(GetParameterNames),
      _ => EmptyList<IEnumerable<(string, ITreeNode)>>.Enumerable
    };
}
