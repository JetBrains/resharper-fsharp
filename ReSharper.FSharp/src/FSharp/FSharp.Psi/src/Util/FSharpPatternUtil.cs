using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

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

    [NotNull]
    public static ConstantValue GetConstantValue([CanBeNull] this IReferencePat pat)
    {
      var fcsSymbol = pat?.Reference?.GetFcsSymbol();
      if (fcsSymbol is FSharpMemberOrFunctionOrValue { LiteralValue: { Value: { } mfvValue } } mfv)
        return ConstantValue.Create(mfvValue, mfv.FullType.MapType(pat));

      if (fcsSymbol is FSharpField  { LiteralValue: { Value: { } fieldValue } } field)
        return ConstantValue.Create(fieldValue, field.FieldType.MapType(pat));
      
      return ConstantValue.NOT_COMPILE_TIME_CONSTANT;
    }
  }
}
