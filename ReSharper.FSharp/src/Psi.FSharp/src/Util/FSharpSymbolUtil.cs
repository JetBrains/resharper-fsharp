using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Impl;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public class FSharpSymbolUtil
  {
    private const string FSharpConstructorName = "( .ctor )";
    private const string ClrConstructorName = ".ctor";
    private const string AttributeSuffix = "Attribute";

    private static readonly ClrTypeName SourceNameAttributeAttr =
      new ClrTypeName("Microsoft.FSharp.Core.CompilationSourceNameAttribute");

    private static readonly ClrTypeName CompilationRepresentationAttr =
      new ClrTypeName("Microsoft.FSharp.Core.CompilationRepresentationAttribute");

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
          names.Add(typeShortName.SubstringBeforeLast(AttributeSuffix));

        var abbreviatedTypes = FSharpTypeAbbreviationsUtil.AbbreviatedTypes;
        names.AddRange(abbreviatedTypes.TryGetValue(type.GetClrName(), EmptyArray<string>.Instance));
      }
      var attrOwner = element as IAttributesOwner;
      if (attrOwner != null)
      {
        var sourceName = GetAttributeValue(attrOwner, SourceNameAttributeAttr) as string;
        if (sourceName != null) names.Add(sourceName);

        var compilationRepr = GetAttributeValue(attrOwner, CompilationRepresentationAttr) as int?;
        if (CompilationRepresentationFlags.ModuleSuffix.Equals(compilationRepr))
          names.Add(element.ShortName.SubstringBeforeLast("Module"));
      }

      var fsSymbol = (element as FSharpFakeElementFromReference)?.Symbol;
      if (fsSymbol == null) return names;

      var entity = fsSymbol as FSharpEntity;
      if (entity != null)
      {
        while (entity.IsFSharpAbbreviation)
        {
          var abbreviatedType = entity.AbbreviatedType;
          if (!abbreviatedType.HasTypeDefinition) break;

          entity = abbreviatedType.TypeDefinition;
          names.Add(entity.DisplayName);
        }
      }
      return names;
    }

    [CanBeNull]
    private static object GetAttributeValue([NotNull] IAttributesOwner attrOwner, [NotNull] IClrTypeName attrName)
    {
      var attrInstance = attrOwner.GetAttributeInstances(attrName, true).FirstOrDefault();
      return attrInstance?.PositionParameters().FirstOrDefault()?.ConstantValue.Value;
    }

    [NotNull]
    public static string GetEntityHighlightingAttributeId([NotNull] FSharpEntity entity)
    {
      if (entity.IsNamespace) return HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE;
      if (entity.IsInterface) return HighlightingAttributeIds.TYPE_INTERFACE_ATTRIBUTE;

      return HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE;
    }

    [NotNull]
    public static string GetMfvHighlightingAttributeId([NotNull] FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsEvent || mfv.IsEventAddMethod || mfv.IsEventRemoveMethod)
        return HighlightingAttributeIds.EVENT_IDENTIFIER_ATTRIBUTE;
      if (mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod)
        return HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE;

      var name = mfv.DisplayName;
      if (mfv.IsImplicitConstructor || name == ClrConstructorName || name == FSharpConstructorName)
        return HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE;

      return HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE;
    }

    [NotNull]
    public static string GetHighlightingAttributeId([NotNull] FSharpSymbol symbol)
    {
      var entity = symbol as FSharpEntity;
      if (entity != null) return GetEntityHighlightingAttributeId(entity);

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null) return GetMfvHighlightingAttributeId(mfv);

      if (symbol is FSharpField) return HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE;
      if (symbol is FSharpUnionCase) return HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE;

      return HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE;
    }
  }
}