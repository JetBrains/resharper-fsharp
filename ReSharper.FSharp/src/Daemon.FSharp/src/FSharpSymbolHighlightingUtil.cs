﻿using FSharp.Compiler.SourceCodeServices;
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
        return FSharpHighlightingAttributeIdsModule.Namespace;

      if (entity.IsEnum)
        return FSharpHighlightingAttributeIdsModule.Enum;

      if (entity.IsValueType)
        return FSharpHighlightingAttributeIdsModule.Struct;

      if (entity.IsDelegate)
        return FSharpHighlightingAttributeIdsModule.Delegate;

      if (entity.IsFSharpModule)
        return FSharpHighlightingAttributeIdsModule.Module;

      if (entity.IsFSharpUnion)
        return FSharpHighlightingAttributeIdsModule.Union;

      if (entity.IsFSharpRecord)
        return FSharpHighlightingAttributeIdsModule.Record;

      if (entity.IsMeasure)
        return FSharpHighlightingAttributeIdsModule.Measure;
      
      return entity.IsInterface
        ? FSharpHighlightingAttributeIdsModule.Interface
        : FSharpHighlightingAttributeIdsModule.Class;
    }

    [NotNull]
    public static string GetMfvHighlightingAttributeId([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsEvent || mfv.IsEventAddMethod || mfv.IsEventRemoveMethod || mfv.EventForFSharpProperty != null)
        return FSharpHighlightingAttributeIdsModule.Event;

      if (mfv.IsImplicitConstructor || mfv.IsConstructor)
        return mfv.DeclaringEntity?.Value is FSharpEntity declEntity && declEntity.IsValueType
          ? FSharpHighlightingAttributeIdsModule.Struct
          : FSharpHighlightingAttributeIdsModule.Class;

      var entity = mfv.DeclaringEntity;
      if (mfv.IsModuleValueOrMember && (entity != null && !entity.Value.IsFSharpModule || mfv.IsExtensionMember))
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

      if (IsMangledOpName(mfv.LogicalName))
        return FSharpHighlightingAttributeIdsModule.Operator;

      if (mfv.IsValCompiledAsMethod)
        return FSharpHighlightingAttributeIdsModule.Method;
      
      if (mfv.FullType.IsFunctionType)
        return mfv.IsMutable
          ? FSharpHighlightingAttributeIdsModule.MutableValueFunction
          : FSharpHighlightingAttributeIdsModule.ValueFunction;

      if (mfv.IsMutable || mfv.IsRefCell())
        return FSharpHighlightingAttributeIdsModule.MutableValue;

      var fsType = mfv.FullType;
      if (fsType.HasTypeDefinition && fsType.TypeDefinition is var mfvTypeEntity && mfvTypeEntity.IsByRef)
        return FSharpHighlightingAttributeIdsModule.MutableValue;

      return FSharpHighlightingAttributeIdsModule.Value;
    }

    [NotNull]
    public static string GetHighlightingAttributeId([NotNull] this FSharpSymbol symbol)
    {
      switch (symbol)
      {
        case FSharpEntity entity when !entity.IsUnresolved:
          return GetEntityHighlightingAttributeId(entity);

        case FSharpParameter parameter: // FSharpParameter is a static member in a generic constraint, not a method parameter
          if (parameter.FullName.Equals("new"))
            return FSharpHighlightingAttributeIdsModule.Keyword;
          return parameter.Type.GenericArguments.Count == 2 && parameter.Type.GenericArguments[0].IsUnit
            ? FSharpHighlightingAttributeIdsModule.Property
            : FSharpHighlightingAttributeIdsModule.Method;
        
        case FSharpMemberOrFunctionOrValue mfv when !mfv.IsUnresolved:
          return GetMfvHighlightingAttributeId(mfv.AccessorProperty?.Value ?? mfv);

        case FSharpField field:
          return field.IsLiteral
            ? FSharpHighlightingAttributeIdsModule.Literal
            : FSharpHighlightingAttributeIdsModule.Field;

        case FSharpUnionCase _:
          return FSharpHighlightingAttributeIdsModule.UnionCase;

        case FSharpGenericParameter _:
          return FSharpHighlightingAttributeIdsModule.TypeParameter;

        case FSharpActivePatternCase _:
          return FSharpHighlightingAttributeIdsModule.ActivePatternCase;
      }

      // some highlighting is needed for tooltip provider
      return FSharpHighlightingAttributeIdsModule.Value;
    }
  }
}
