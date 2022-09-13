using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using static FSharp.Compiler.Syntax.PrettyNaming;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon
{
  // todo: provide F# specific highlightings and use semantic classification from FCS
  public static class FSharpSymbolHighlightingUtil
  {
    [NotNull]
    public static string GetEntityHighlightingAttributeId([NotNull] this FSharpEntity entity)
    {
      if (entity.IsNamespace)
        return FSharpHighlightingAttributeIdsModule.Namespace;

      if (entity.IsEnum)
        return FSharpHighlightingAttributeIdsModule.Enum;

      if (entity.IsDelegate)
        return FSharpHighlightingAttributeIdsModule.Delegate;

      if (entity.IsFSharpModule)
        return FSharpHighlightingAttributeIdsModule.Module;

      if (entity.IsFSharpUnion)
        return FSharpHighlightingAttributeIdsModule.Union;

      if (entity.IsFSharpRecord)
        return FSharpHighlightingAttributeIdsModule.Record;

      if (entity.IsMeasure)
        return FSharpHighlightingAttributeIdsModule.UnitOfMeasure;

      if (entity.IsInterface)
        return FSharpHighlightingAttributeIdsModule.Interface;

      if (entity.IsClass)
        return FSharpHighlightingAttributeIdsModule.Class;

      if (entity.IsValueType || entity.HasMeasureParameter())
        return FSharpHighlightingAttributeIdsModule.Struct;

      if (entity.IsFSharpAbbreviation && entity.AbbreviatedType.IsFunctionType)
        return FSharpHighlightingAttributeIdsModule.Delegate;

      return FSharpHighlightingAttributeIdsModule.Class;
    }

    [NotNull]
    public static string GetMfvHighlightingAttributeId([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsEvent || mfv.IsEventAddMethod || mfv.IsEventRemoveMethod || mfv.EventForFSharpProperty != null)
        return FSharpHighlightingAttributeIdsModule.Event;

      if (mfv.IsImplicitConstructor || mfv.IsConstructor)
        return mfv.DeclaringEntity?.Value is { IsValueType: true }
          ? FSharpHighlightingAttributeIdsModule.Struct
          : FSharpHighlightingAttributeIdsModule.Class;

      var entity = mfv.DeclaringEntity;
      if (mfv.IsMember && (entity != null && !entity.Value.IsFSharpModule || mfv.IsExtensionMember))
        if (mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod)
          return mfv.IsExtensionMember
            ? FSharpHighlightingAttributeIdsModule.ExtensionProperty
            : FSharpHighlightingAttributeIdsModule.Property;
        else
          return mfv.IsExtensionMember
            ? FSharpHighlightingAttributeIdsModule.ExtensionMethod
            : FSharpHighlightingAttributeIdsModule.Method;

      if (mfv.LiteralValue != null)
        return FSharpHighlightingAttributeIdsModule.Literal;

      if (mfv.IsActivePattern)
        return FSharpHighlightingAttributeIdsModule.ActivePatternCase;

      if (IsLogicalOpName(mfv.LogicalName))
        return FSharpHighlightingAttributeIdsModule.Operator;

      var fcsType = mfv.FullType;
      if (fcsType.IsFunctionType || mfv.IsTypeFunction || fcsType.IsAbbreviation && fcsType.AbbreviatedType.IsFunctionType)
        return mfv.IsMutable
          ? FSharpHighlightingAttributeIdsModule.MutableFunction
          : FSharpHighlightingAttributeIdsModule.Function;

      if (mfv.IsMutable || mfv.IsRefCell())
        return FSharpHighlightingAttributeIdsModule.MutableValue;

      if (fcsType.HasTypeDefinition && fcsType.TypeDefinition is var mfvTypeEntity && mfvTypeEntity.IsByRef)
        return FSharpHighlightingAttributeIdsModule.MutableValue;

      return FSharpHighlightingAttributeIdsModule.Value;
    }

    [NotNull]
    public static string GetHighlightingAttributeId([NotNull] this FSharpSymbol symbol) =>
      symbol switch
      {
        FSharpEntity { IsUnresolved: false } entity => GetEntityHighlightingAttributeId(entity.GetAbbreviatedEntity()),

        FSharpMemberOrFunctionOrValue { IsUnresolved: false } mfv => GetMfvHighlightingAttributeId(
          mfv.AccessorProperty?.Value ?? mfv),

        FSharpField field => field.IsLiteral
          ? FSharpHighlightingAttributeIdsModule.Literal
          : FSharpHighlightingAttributeIdsModule.Field,

        FSharpUnionCase _ => FSharpHighlightingAttributeIdsModule.UnionCase,
        FSharpGenericParameter _ => FSharpHighlightingAttributeIdsModule.TypeParameter,
        FSharpActivePatternCase _ => FSharpHighlightingAttributeIdsModule.ActivePatternCase,

        // some highlighting is needed for tooltip provider
        _ => FSharpHighlightingAttributeIdsModule.Value
      };
  }
}
