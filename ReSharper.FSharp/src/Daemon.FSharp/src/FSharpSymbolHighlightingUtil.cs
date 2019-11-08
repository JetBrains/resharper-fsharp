using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using static FSharp.Compiler.PrettyNaming;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs
{
  // todo: provide F# specific highlightings and use semantic classification from FCS
  public static class FSharpSymbolHighlightingUtil
  {
    [NotNull]
    public static string GetEntityHighlightingAttributeId([NotNull] this FSharpEntity entity)
    {
      if (entity.IsNamespace)
        return HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE;

      if (entity.IsEnum)
        return HighlightingAttributeIds.TYPE_ENUM_ATTRIBUTE;

      if (entity.IsValueType)
        return HighlightingAttributeIds.TYPE_STRUCT_ATTRIBUTE;

      if (entity.IsDelegate)
        return HighlightingAttributeIds.TYPE_DELEGATE_ATTRIBUTE;

      if (entity.IsFSharpModule)
        return HighlightingAttributeIds.TYPE_STATIC_CLASS_ATTRIBUTE;

      return entity.IsInterface
        ? HighlightingAttributeIds.TYPE_INTERFACE_ATTRIBUTE
        : HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE;
    }

    [NotNull]
    public static string GetMfvHighlightingAttributeId([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsEvent || mfv.IsEventAddMethod || mfv.IsEventRemoveMethod || mfv.EventForFSharpProperty != null)
        return HighlightingAttributeIds.EVENT_IDENTIFIER_ATTRIBUTE;

      if (mfv.IsImplicitConstructor || mfv.IsConstructor)
        return mfv.DeclaringEntity?.Value is FSharpEntity declEntity && declEntity.IsValueType
          ? HighlightingAttributeIds.TYPE_STRUCT_ATTRIBUTE
          : HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE;

      var entity = mfv.DeclaringEntity;
      if (mfv.IsModuleValueOrMember && (entity != null && !entity.Value.IsFSharpModule || mfv.IsExtensionMember))
        return mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod
          ? HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE
          : HighlightingAttributeIds.METHOD_IDENTIFIER_ATTRIBUTE;

      if (mfv.LiteralValue != null)
        return HighlightingAttributeIds.CONSTANT_IDENTIFIER_ATTRIBUTE;

      if (mfv.IsActivePattern)
        return HighlightingAttributeIds.METHOD_IDENTIFIER_ATTRIBUTE;

      if (mfv.IsMutable || mfv.IsRefCell())
        return HighlightingAttributeIds.MUTABLE_LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE;

      if (IsMangledOpName(mfv.LogicalName))
        return HighlightingAttributeIds.OPERATOR_IDENTIFIER_ATTRIBUTE;

      var fsType = mfv.FullType;
      if (fsType.HasTypeDefinition && fsType.TypeDefinition is var mfvTypeEntity && mfvTypeEntity.IsByRef)
        return HighlightingAttributeIds.MUTABLE_LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE;

      return HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE;
    }

    [NotNull]
    public static string GetHighlightingAttributeId([NotNull] this FSharpSymbol symbol)
    {
      switch (symbol)
      {
        case FSharpEntity entity when !entity.IsUnresolved:
          return GetEntityHighlightingAttributeId(entity);

        case FSharpMemberOrFunctionOrValue mfv when !mfv.IsUnresolved:
          return GetMfvHighlightingAttributeId(mfv.AccessorProperty?.Value ?? mfv);

        case FSharpField field:
          return field.IsLiteral
            ? HighlightingAttributeIds.CONSTANT_IDENTIFIER_ATTRIBUTE
            : HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE;

        case FSharpUnionCase _:
          return HighlightingAttributeIds.TYPE_ENUM_ATTRIBUTE;

        case FSharpGenericParameter _:
          return HighlightingAttributeIds.TYPE_PARAMETER_ATTRIBUTE;

        case FSharpActivePatternCase _:
          return HighlightingAttributeIds.METHOD_IDENTIFIER_ATTRIBUTE;
      }

      // some highlighting is needed for tooltip provider
      return HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE;
    }
  }
}
