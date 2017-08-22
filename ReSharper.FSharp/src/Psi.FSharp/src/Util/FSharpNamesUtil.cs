using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public class FSharpNamesUtil
  {
    private const string FSharpConstructorName = "( .ctor )";
    private const string ClrConstructorName = ".ctor";
    private const string AttributeSuffix = "Attribute";
    private const string ModuleSuffix = "Module";
    private const int EscapedNameAffixLength = 4;
    private const int EscapedNameStartIndex = 2;
    private static readonly int ModuleSuffixFlag = (int) CompilationRepresentationFlags.ModuleSuffix;

    private static readonly ClrTypeName SourceNameAttributeAttr =
      new ClrTypeName("Microsoft.FSharp.Core.CompilationSourceNameAttribute");

    private static readonly ClrTypeName CompilationRepresentationAttr =
      new ClrTypeName("Microsoft.FSharp.Core.CompilationRepresentationAttribute");

    public static bool IsEscapedWithParens([NotNull] string name)
    {
      var length = name.Length;
      return length > EscapedNameAffixLength &&
             name[0] == '(' && name[1] == ' ' && name[length - 2] == ' ' && name[length - 1] == ')';
    }

    public static bool IsEscapedWithBackticks([NotNull] string name)
    {
      var length = name.Length;
      return length > EscapedNameAffixLength &&
             name[0] == '`' && name[1] == '`' && name[length - 2] == '`' && name[length - 1] == '`';
    }

    [NotNull]
    public static string RemoveBackticks([NotNull] string name)
    {
      return IsEscapedWithBackticks(name)
        ? name.Substring(EscapedNameStartIndex, name.Length - EscapedNameAffixLength)
        : name;
    }

    [NotNull]
    public static string RemoveParens([NotNull] string name, out bool isEscaped)
    {
      isEscaped = IsEscapedWithParens(name);
      return isEscaped
        ? name.Substring(EscapedNameStartIndex, name.Length - EscapedNameAffixLength)
        : name;
    }

    [NotNull]
    public static IEnumerable<string> GetPossibleSourceNames([NotNull] IDeclaredElement element)
    {
      var names = new List<string>();

      var constructor = element as IConstructor;
      var typeElement = constructor?.GetContainingType();
      if (typeElement != null) names.Add(typeElement.ShortName);

      names.Add(element.ShortName);

      var type = element as ITypeElement;
      if (type != null)
      {
        var typeShortName = type.ShortName;
        if (typeShortName.EndsWith(AttributeSuffix))
          names.Add(typeShortName.SubstringBeforeLast(AttributeSuffix, StringComparison.Ordinal));

        var abbreviatedTypes = FSharpTypeAbbreviationsUtil.AbbreviatedTypes;
        names.AddRange(abbreviatedTypes.TryGetValue(type.GetClrName(), EmptyArray<string>.Instance));
      }
      var attrOwner = element as IAttributesOwner;
      if (attrOwner != null)
      {
        var sourceName = GetAttributeValue(attrOwner, SourceNameAttributeAttr) as string;
        if (sourceName != null) names.Add(sourceName);

        var reprFlag = GetAttributeValue(attrOwner, CompilationRepresentationAttr) as int?;
        if (reprFlag != null && reprFlag.Value == ModuleSuffixFlag)
          names.Add(element.ShortName.SubstringBeforeLast(ModuleSuffix, StringComparison.Ordinal));
        // todo: implicit module suffix in F# 4.1
      }

      foreach (var declaration in element.GetDeclarations())
      {
        var fsDeclaration = declaration as IFSharpDeclaration;
        if (fsDeclaration != null)
          names.Add(fsDeclaration.SourceName);
      }

      // todo: type abbreviations
      return names;
    }

    [CanBeNull]
    private static object GetAttributeValue([NotNull] IAttributesSet attrs, [NotNull] IClrTypeName attrName)
    {
      var attrInstance = attrs.GetAttributeInstances(attrName, true).FirstOrDefault();
      return attrInstance?.PositionParameters().FirstOrDefault()?.ConstantValue.Value;
    }
  }
}