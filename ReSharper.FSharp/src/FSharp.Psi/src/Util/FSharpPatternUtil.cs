using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpPatternUtil
  {
    [ItemNotNull]
    public static IEnumerable<IFSharpPattern> GetPartialDeclarations([NotNull] this IFSharpPattern fsPattern)
    {
      if (!(fsPattern is IReferencePat refPat))
        return new[] { fsPattern };

      var canBePartial = false;

      while (fsPattern.Parent is IFSharpPattern parent)
      {
        fsPattern = parent;

        if (parent is IOrPat || parent is IAndsPat)
          canBePartial = true;
      }

      if (!canBePartial)
        return new[] { refPat };

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

    private static IFSharpPattern GetPossibleContainingParameterPattern([CanBeNull] this IFSharpPattern pat)
    {
      while (true)
      {
        pat = pat.IgnoreParentParens();

        var typedPat = TypedPatNavigator.GetByPattern(pat);
        if (typedPat != null)
        {
          pat = typedPat;
          continue;
        }

        var attribPat = AttribPatNavigator.GetByPattern(pat);
        if (attribPat != null)
        {
          pat = attribPat;
          continue;
        }

        var optionalPat = OptionalValPatNavigator.GetByPattern(pat);
        if (optionalPat != null)
        {
          pat = optionalPat;
          continue;
        }

        return pat;
      }
    }

    [CanBeNull]
    public static IFSharpParameterDeclaration GetParameterDeclaration([CanBeNull] this IFSharpPattern pat)
    {
      if (pat == null)
        return null;

      pat = GetPossibleContainingParameterPattern(pat);

      var topLevelPat = TuplePatNavigator.GetByPattern(pat) ?? pat;
      var decl = ParametersPatternDeclarationNavigator.GetByPattern(topLevelPat.IgnoreParentParens());
      if (decl == null)
        return null;

      var declPat = decl.Pattern;
      return declPat == pat || declPat is IParenPat parenPat && parenPat == pat || decl.IgnoresIntermediateParens
        ? (IFSharpParameterDeclaration)pat // todo
        : null;
    }

    public static IParametersPatternDeclaration GetParameterGroupDecl([CanBeNull] this IFSharpPattern pat)
    {
      var tuplePat = TuplePatNavigator.GetByPattern(pat);
      var topLevelPat = tuplePat ?? pat;

      return ParametersPatternDeclarationNavigator.GetByPattern(topLevelPat.IgnoreParentParens());
    }

    public static bool IsParameterDeclaration([CanBeNull] this IFSharpPattern pat)
    {
      var tuplePat = TuplePatNavigator.GetByPattern(pat);
      var topLevelPat = tuplePat ?? pat;

      var decl = ParametersPatternDeclarationNavigator.GetByPattern(topLevelPat.IgnoreParentParens());
      return decl != null;
    }

    // public static IFSharpParameter GetParameter([CanBeNull] this IFSharpPattern pat)
    // {
    //   
    // }
  }
}
