using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class XmlDocTemplateUtil
  {
    [NotNull]
    public static string GetDocTemplate(IDeclaration owner, Func<int, string> linePrefix, string lineSeparator)
    {
      // Check owner on null

      var text = new StringBuilder();

      text.Append($"summary>{lineSeparator}");
      var line = 1;
      text.Append($"{linePrefix(line++)}{lineSeparator}");
      text.Append($"{linePrefix(line++)}</summary>{lineSeparator}");

      foreach (var parameter in GetParameters(owner))
        text.Append($"{linePrefix(line++)}<param name=\"{parameter}\"></param>{lineSeparator}");

      return text.ToString();
    }

    //TODO: abstract members
    //TODO: proerties
    private static IEnumerable<string> GetParameters(IDeclaration declaration)
    {
      if (declaration is IBinding binding)
      {
        var list = new List<string>();
        list.AddRange(binding.ParameterPatterns.SelectMany(GetParametersFromParameter));
        if (binding.Expression is ILambdaExpr lambda) GetLambdaArgs(lambda, list);
        return list;
      }

      if (declaration is IMemberDeclaration member)
      {
        return member.ParameterPatterns.SelectMany(GetParametersFromParameter)
          .Union(
            member.AccessorDeclarations.SelectMany(t => t.ParameterPatterns.SelectMany(GetParametersFromParameter)));
      }

      if (declaration is IAbstractMemberDeclaration memberSign)
      {
        return GetParametersFromParameter(memberSign.ReturnTypeInfo.ReturnType);
      }

      if (declaration is IUnionCaseDeclaration ucDecl)
        return ucDecl.Fields.Select(t => t.SourceName).Where(t => t != SharedImplUtil.MISSING_DECLARATION_NAME);

      return EmptyList<string>.Enumerable;
    }

    private static void GetLambdaArgs(ILambdaExpr expr, List<string> pats)
    {
      // IgnoreInnerParens()
      pats.AddRange(expr.PatternsEnumerable.SelectMany(GetParametersFromParameter));
      if (expr.Expression is ILambdaExpr innerLambda) GetLambdaArgs(innerLambda, pats);
      return;
    }

    private static IEnumerable<string> GetParametersFromParameter(IFSharpPattern pattern)
    {
      pattern = pattern.IgnoreInnerParens();
      if (pattern is ILocalReferencePat local) return new[] { local.SourceName };
      if (pattern is ITypedPat typed) return GetParametersFromParameter(typed.Pattern);
      if (pattern is IAsPat asPat) return GetParametersFromParameter(asPat.RightPattern);
      if (pattern is ITuplePat tuplePat) return tuplePat.PatternsEnumerable.SelectMany(GetParametersFromParameter);
      return EmptyList<string>.Enumerable;
    }

    private static IEnumerable<string> GetParametersFromParameter(ITypeUsage pattern)
    {
      if (pattern is IParenTypeUsage parenUsage) return GetParametersFromParameter(parenUsage.InnerTypeUsage);
      if (pattern is IParameterSignatureTypeUsage { Identifier: not null } local)
        return new[] { local.Identifier.Name };
      if (pattern is IFunctionTypeUsage funPat)
        return GetParametersFromParameter(funPat.ArgumentTypeUsage)
          .Union(GetParametersFromParameter(funPat.ReturnTypeUsage));
      if (pattern is ITupleTypeUsage tuplePat) return tuplePat.Items.SelectMany(GetParametersFromParameter);
      return EmptyList<string>.Enumerable;
    }
  }
}
