using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class XmlDocTemplateUtil
  {
    [NotNull]
    public static string GetDocTemplate(ITreeNode owner, Func<int, string> linePrefix, string lineSeparator)
    {
      var text = new StringBuilder();
      var line = 1;

      text.Append($"summary>{lineSeparator}");
      text.Append($"{linePrefix(line++)}{lineSeparator}");
      text.Append($"{linePrefix(line++)}</summary>{lineSeparator}");

      foreach (var parameter in GetParameters(owner))
        text.Append($"{linePrefix(line++)}<param name=\"{parameter}\"></param>{lineSeparator}");

      return text.ToString();
    }

    private static IEnumerable<string> GetParameters(ITreeNode declaration) =>
      declaration switch
      {
        IBinding binding => binding.Expression is ILambdaExpr lambda
          ? binding.ParameterPatterns.SelectMany(t => GetParameterNames(t, false)).Union(GetLambdaArgs(lambda))
          : binding.ParameterPatterns.SelectMany(t => GetParameterNames(t, false)),
        IBindingSignature bindingSignature => GetParameterNames(bindingSignature.ReturnTypeInfo.ReturnType),
        IMemberDeclaration member => member.ParameterPatterns.SelectMany(t => GetParameterNames(t, true))
          .Union(member.AccessorDeclarations.SelectMany(t =>
            t.ParameterPatterns.SelectMany(t => GetParameterNames(t, true)))),
        IConstructorSignature constructorSignature => GetParameterNames(constructorSignature.ReturnTypeInfo.ReturnType),
        IConstructorDeclaration constructorDeclaration => GetParameterNames(constructorDeclaration.ParameterPatterns,
          true),
        IAbstractMemberDeclaration abstractMember => GetParameterNames(abstractMember.ReturnTypeInfo.ReturnType),
        IMemberSignature memberSignature => GetParameterNames(((IMemberSignatureOrDeclaration)memberSignature)
          .ReturnTypeInfo.ReturnType),
        IUnionCaseDeclaration ucDecl => ucDecl.Fields.Select(t => t.SourceName)
          .Where(t => t != SharedImplUtil.MISSING_DECLARATION_NAME),
        _ => EmptyList<string>.Enumerable
      };

    private static IEnumerable<string> GetLambdaArgs(ILambdaExpr expr)
    {
      var parameters = expr.PatternsEnumerable.SelectMany(t => GetParameterNames(t, false));
      if (expr.Expression is ILambdaExpr innerLambda)
        parameters = parameters.Union(GetLambdaArgs(innerLambda));
      return parameters;
    }

    private static IEnumerable<string> GetParameterNames(IFSharpPattern pattern, bool isMember)
    {
      IEnumerable<string> GetParameterNamesInternal(IFSharpPattern pattern, bool isMember, bool isTopLevelArg)
      {
        pattern = pattern.IgnoreInnerParens();
        return pattern switch
        {
          ILocalReferencePat local => new[] { local.SourceName },
          ITypedPat typed => GetParameterNamesInternal(typed.Pattern, isMember, false),
          IAttribPat attrib => GetParameterNamesInternal(attrib.Pattern, isMember, false),
          IAsPat asPat => GetParameterNamesInternal(asPat.RightPattern, isMember, false),
          ITuplePat tuplePat => isMember && !isTopLevelArg
            ? EmptyList<string>.Enumerable
            : tuplePat.PatternsEnumerable.SelectMany(t => GetParameterNamesInternal(t, true, false)),
          _ => EmptyList<string>.Enumerable
        };
      }

      return GetParameterNamesInternal(pattern, isMember, true);
    }

    private static IEnumerable<string> GetParameterNames(ITypeUsage pattern) =>
      pattern switch
      {
        IParenTypeUsage parenUsage => GetParameterNames(parenUsage.InnerTypeUsage),
        IParameterSignatureTypeUsage { Identifier: not null } local => new[] { local.Identifier.Name },
        IFunctionTypeUsage funPat => GetParameterNames(funPat.ArgumentTypeUsage)
          .Union(GetParameterNames(funPat.ReturnTypeUsage)),
        ITupleTypeUsage tuplePat => tuplePat.Items.SelectMany(GetParameterNames),
        _ => EmptyList<string>.Enumerable
      };
  }
}
