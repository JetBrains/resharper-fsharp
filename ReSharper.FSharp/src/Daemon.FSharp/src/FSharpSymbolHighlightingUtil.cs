using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings;
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
        return FSharpHighlightingAttributeIds.Namespace;

      if (entity.IsEnum)
        return FSharpHighlightingAttributeIds.Enum;

      if (entity.IsValueType)
        return FSharpHighlightingAttributeIds.Struct;

      if (entity.IsDelegate)
        return FSharpHighlightingAttributeIds.Delegate;

      if (entity.IsFSharpModule)
        return FSharpHighlightingAttributeIds.Module;

      if (entity.IsFSharpUnion)
        return FSharpHighlightingAttributeIds.Union;

      if (entity.IsFSharpRecord)
        return FSharpHighlightingAttributeIds.Record;

      return entity.IsInterface
        ? FSharpHighlightingAttributeIds.Interface
        : FSharpHighlightingAttributeIds.Class;
    }

    [NotNull]
    public static string GetMfvHighlightingAttributeId([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsEvent || mfv.IsEventAddMethod || mfv.IsEventRemoveMethod || mfv.EventForFSharpProperty != null)
        return FSharpHighlightingAttributeIds.Event;

      if (mfv.IsImplicitConstructor || mfv.IsConstructor)
        return mfv.DeclaringEntity?.Value is FSharpEntity declEntity && declEntity.IsValueType
          ? FSharpHighlightingAttributeIds.Struct
          : FSharpHighlightingAttributeIds.Class;

      var entity = mfv.DeclaringEntity;
      if (mfv.IsModuleValueOrMember && (entity != null && !entity.Value.IsFSharpModule || mfv.IsExtensionMember))
        return mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod
          ? FSharpHighlightingAttributeIds.Property
          : FSharpHighlightingAttributeIds.Method;

      if (mfv.LiteralValue != null)
        return FSharpHighlightingAttributeIds.Literal;

      if (mfv.IsActivePattern)
        return FSharpHighlightingAttributeIds.ActivePatternCase;

      if (mfv.IsMutable || mfv.IsRefCell())
        return FSharpHighlightingAttributeIds.MutableValue;

      if (IsMangledOpName(mfv.LogicalName))
        return FSharpHighlightingAttributeIds.Operator;

      var fsType = mfv.FullType;
      if (fsType.HasTypeDefinition && fsType.TypeDefinition is var mfvTypeEntity && mfvTypeEntity.IsByRef)
        return FSharpHighlightingAttributeIds.MutableValue;

      return FSharpHighlightingAttributeIds.Value;
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
            ? FSharpHighlightingAttributeIds.Literal
            : FSharpHighlightingAttributeIds.Field;

        case FSharpUnionCase _:
          return FSharpHighlightingAttributeIds.UnionCase;

        case FSharpGenericParameter _:
          return FSharpHighlightingAttributeIds.TypeParameter;

        case FSharpActivePatternCase _:
          return FSharpHighlightingAttributeIds.ActivePatternCase;
      }

      // some highlighting is needed for tooltip provider
      return FSharpHighlightingAttributeIds.Value;
    }
  }
}
