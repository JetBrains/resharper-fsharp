using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpNamesUtil
  {
    private const string ModuleSuffix = "Module";
    private const int EscapedNameAffixLength = 4;
    private const int EscapedNameStartIndex = 2;
    private const int ModuleSuffixFlag = (int) CompilationRepresentationFlags.ModuleSuffix;

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
    public static string RemoveBackticks([NotNull] this  string name) =>
      IsEscapedWithBackticks(name)
        ? name.Substring(EscapedNameStartIndex, name.Length - EscapedNameAffixLength)
        : name;

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
      var names = new HashSet<string> {element.ShortName};

      var typeElement = (element as IConstructor)?.GetContainingType();
      if (typeElement != null)
        GetPossibleSourceNames(typeElement, names);

      if (element is ITypeElement type)
        GetPossibleSourceNames(type, names);

      if (element is IAttributesOwner attrOwner)
      {
        if (GetAttributeValue(attrOwner, SourceNameAttributeAttr) is string sourceName)
          names.Add(sourceName);
        if (GetAttributeValue(attrOwner, CompilationRepresentationAttr) is int reprFlag && reprFlag == ModuleSuffixFlag)
          names.Add(element.ShortName.SubstringBeforeLast(ModuleSuffix, StringComparison.Ordinal));
      }

      foreach (var declaration in element.GetDeclarations())
        if (declaration is IFSharpDeclaration fsDeclaration)
          names.Add(fsDeclaration.SourceName);

      var result = new List<string>(names.Count * 2);
      result.AddRange(names);
      result.AddRange(names.Select(n => $"``{n}``"));

      return result;
    }

    private static void GetPossibleSourceNames(ITypeElement type, ISet<string> names)
    {
      names.Add(type.ShortName);

      var typeShortName = type.ShortName;
      if (typeShortName.EndsWith(FSharpImplUtil.AttributeSuffix))
        names.Add(typeShortName.SubstringBeforeLast(FSharpImplUtil.AttributeSuffix, StringComparison.Ordinal));

      if (type.GetClrName().TryGetAbbreviations(out var abbreviations))
        names.AddRange(abbreviations);
    }

    [CanBeNull]
    private static object GetAttributeValue([NotNull] IAttributesSet attrs, [NotNull] IClrTypeName attrName) =>
      attrs.GetAttributeInstances(attrName, true).FirstOrDefault()?.PositionParameters()
        .FirstOrDefault()?.ConstantValue.Value;
  }
}