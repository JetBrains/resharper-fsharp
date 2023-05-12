using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Core;
using static FSharp.Compiler.Syntax.PrettyNaming;
using static JetBrains.ReSharper.Plugins.FSharp.Util.FSharpPredefinedType;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpNamesUtil
  {
    public const int EscapedNameAffixLength = 4;
    public const int EscapedNameStartIndex = 2;

    /// Checks if an identifier is escaped, like in ``foo bar``.
    /// Synthetic FCS symbols may have escaped empty names, like ````.
    public static bool IsEscapedWithBackticks([NotNull] this string name)
    {
      var length = name.Length;
      return length >= EscapedNameAffixLength &&
             name[0] == '`' && name[1] == '`' && name[length - 2] == '`' && name[length - 1] == '`';
    }

    [NotNull]
    public static string RemoveBackticks([NotNull] this string name) =>
      IsEscapedWithBackticks(name)
        ? name.Substring(EscapedNameStartIndex, name.Length - EscapedNameAffixLength)
        : name;

    [NotNull]
    public static IEnumerable<string> GetPossibleSourceNames([NotNull] IDeclaredElement element)
    {
      var name = element.ShortName;
      var names = new HashSet<string> {name, ConvertValLogicalNameToDisplayNameCore(name)};

      if (element is IFSharpDeclaredElement fsDeclaredElement)
        names.Add(fsDeclaredElement.SourceName);

      if (element is IConstructor { ContainingType: { } typeElement })
        GetPossibleSourceNames(typeElement, names);

      if (element is ITypeElement type)
        GetPossibleSourceNames(type, names);

      if (element is IAttributesOwner attrOwner)
      {
        if (GetFirstArgStringValue(attrOwner, SourceNameAttrTypeName) is { } sourceName)
          names.Add(sourceName);

        if (GetFirstArgStringValue(attrOwner, CustomOperationAttrTypeName) is { } customOpName)
          names.Add(customOpName);

        if (GetFirstArgValue(attrOwner, CompilationMappingAttrTypeName) is { } flagValue)
        {
          if ((SourceConstructFlags) flagValue == SourceConstructFlags.UnionCase && element is IMethod &&
              name.StartsWith("New", StringComparison.Ordinal))
            names.Add(name.Substring(3));
        }
      }

      return names;
    }

    private static void GetPossibleSourceNames(ITypeElement type, ISet<string> names)
    {
      names.Add(type.ShortName);

      var typeShortName = type.ShortName;
      names.Add(typeShortName.SubstringBeforeLast(AttributeInstanceExtensions.ATTRIBUTE_SUFFIX));

      if (type.GetClrName().TryGetPredefinedAbbreviations(out var abbreviations))
        names.AddRange(abbreviations);
    }

    private static AttributeValue GetFirstPositionalArg([NotNull] this IAttributesSet attrs, IClrTypeName attrName)
    {
      var instance = attrs.GetAttributeInstances(attrName, false).FirstOrDefault();
      return instance?.PositionParameters().FirstOrDefault();
    }

    [CanBeNull]
    public static object GetFirstArgValue([NotNull] this IAttributesSet attrs, [NotNull] IClrTypeName attrName)
    {
      var positionalArg = GetFirstPositionalArg(attrs, attrName);
      return positionalArg?.ConstantValue.Value;
    }

    [CanBeNull]
    public static string GetFirstArgStringValue([NotNull] this IAttributesSet attrs,
      [NotNull] IClrTypeName attrName)
    {
      var positionalArg = GetFirstPositionalArg(attrs, attrName);
      return positionalArg?.ConstantValue.StringValue;
    }
  }
}
