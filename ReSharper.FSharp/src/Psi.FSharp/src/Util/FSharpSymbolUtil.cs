using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public class FSharpSymbolUtil
  {
    private const string FSharpConstructorName = "( .ctor )";
    private const string ClrConstructorName = ".ctor";
    private const string AttributeSuffix = "Attribute";

    private static readonly ClrTypeName SourceNameAttribute =
      new ClrTypeName("Microsoft.FSharp.Core.CompilationSourceNameAttribute");

    [CanBeNull]
    public static string GetDisplayName(FSharpSymbol symbol)
    {
      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        var name = mfv.DisplayName;
        if (name == FSharpConstructorName || name == ClrConstructorName)
          return mfv.EnclosingEntity.DisplayName;
        if (mfv.IsMember) return name;
      }

      var fsField = symbol as FSharpField;
      if (fsField != null) return fsField.DisplayName;

      return symbol.DeclarationLocation != null ? symbol.DisplayName : null;
    }

    public static bool IsEscapedName([NotNull] string name)
    {
      var length = name.Length;
      return length > 4 && name[0] == '(' && name[1] == ' ' && name[length - 2] == ' ' && name[length - 1] == ')';
    }

    [NotNull]
    public static IEnumerable<string> GetPossibleSourceNames([NotNull] IDeclaredElement element)
    {
      var names = new List<string> {element.ShortName};
      var type = element as ITypeElement;
      if (type != null)
      {
        var typeShortName = type.ShortName;
        if (typeShortName.EndsWith(AttributeSuffix))
          names.Add(typeShortName.SubstringBeforeLast(AttributeSuffix));

        var abbreviatedTypes = FSharpTypeAbbreviationsUtil.AbbreviatedTypes;
        names.AddRange(abbreviatedTypes.TryGetValue(type.GetClrName(), EmptyArray<string>.Instance));
      }
      var attrOwner = element as IAttributesOwner;
      var sourceNameAttr = attrOwner?.GetAttributeInstances(SourceNameAttribute, true).FirstOrDefault();
      var sourceName = sourceNameAttr?.PositionParameters().FirstOrDefault()?.ConstantValue.Value as string;
      if (sourceName != null) names.Add(sourceName);

      return names;
    }
  }
}