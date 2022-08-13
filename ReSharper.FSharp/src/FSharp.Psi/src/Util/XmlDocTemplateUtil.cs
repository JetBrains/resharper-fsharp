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
    private static IEnumerable<string> GetParameters(ITreeNode declaration) =>
      declaration switch
      {
        IBinding binding => binding.Expression is ILambdaExpr lambda
          ? binding.ParameterPatterns.SelectMany(GetParameterNames).Union(GetLambdaArgs(lambda))
          : binding.ParameterPatterns.SelectMany(GetParameterNames),
      
        IBindingSignature bindingSignature => GetParameterNames(bindingSignature.ReturnTypeInfo.ReturnType),

        IMemberDeclaration member => member.ParameterPatterns.SelectMany(GetParameterNames)
          .Union(member.AccessorDeclarations.SelectMany(t => t.ParameterPatterns.SelectMany(GetParameterNames))),
        IConstructorSignature constructorSignature => GetParameterNames(constructorSignature.ReturnTypeInfo.ReturnType),
        IConstructorDeclaration constructorDeclaration => GetParameterNames(constructorDeclaration.ParameterPatterns),

        IAbstractMemberDeclaration abstractMember => GetParameterNames(abstractMember.ReturnTypeInfo.ReturnType),
        IMemberSignature memberSignature => GetParameterNames(memberSignature.TypeUsage),
        IUnionCaseDeclaration ucDecl => ucDecl.Fields.Select(t => t.SourceName)
          .Where(t => t != SharedImplUtil.MISSING_DECLARATION_NAME),
        _ => EmptyList<string>.Enumerable
      };

    private static IEnumerable<string> GetLambdaArgs(ILambdaExpr expr)
    {
      // IgnoreInnerParens()
      var parameters = expr.PatternsEnumerable.SelectMany(GetParameterNames);
      if (expr.Expression is ILambdaExpr innerLambda)
        parameters = parameters.Union(GetLambdaArgs(innerLambda));
      return parameters;
    }

    private static IEnumerable<string> GetParameterNames(IFSharpPattern pattern)
    {
      pattern = pattern.IgnoreInnerParens();
      return pattern switch
      {
        ILocalReferencePat local => new[] { local.SourceName },
        ITypedPat typed => GetParameterNames(typed.Pattern),
        IAttribPat attrib => GetParameterNames(attrib.Pattern),
        IAsPat asPat => GetParameterNames(asPat.RightPattern),
        ITuplePat tuplePat => tuplePat.PatternsEnumerable.SelectMany(GetParameterNames),
        _ => EmptyList<string>.Enumerable
      };
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
