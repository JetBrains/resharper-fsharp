using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Daemon.FSharp
{
  public static class FSharpSymbolHighlightingUtil
  {
    private const string FSharpConstructorName = "( .ctor )";
    private const string ClrConstructorName = ".ctor";

    [NotNull]
    public static string GetEntityHighlightingAttributeId([NotNull] this FSharpEntity entity)
    {
      if (entity.IsNamespace) return HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE;
      if (entity.IsInterface) return HighlightingAttributeIds.TYPE_INTERFACE_ATTRIBUTE;

      return HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE;
    }

    [NotNull]
    public static string GetMfvHighlightingAttributeId([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsEvent || mfv.IsEventAddMethod || mfv.IsEventRemoveMethod)
        return HighlightingAttributeIds.EVENT_IDENTIFIER_ATTRIBUTE;

      var name = mfv.DisplayName;
      if (mfv.IsImplicitConstructor || name == ClrConstructorName || name == FSharpConstructorName)
        return HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE;

      if (mfv.IsMember && (!mfv.EnclosingEntity.IsFSharpModule || mfv.IsExtensionMember))
        return mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod
          ? HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE
          : HighlightingAttributeIds.METHOD_IDENTIFIER_ATTRIBUTE;

      return HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE;
    }

    [NotNull]
    public static string GetHighlightingAttributeId([NotNull] this FSharpSymbol symbol)
    {
      var entity = symbol as FSharpEntity;
      if (entity != null) return GetEntityHighlightingAttributeId(entity);

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null) return GetMfvHighlightingAttributeId(mfv);

      if (symbol is FSharpField) return HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE;
      if (symbol is FSharpUnionCase) return HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE;

      // some highlighting is needed for tooltip provider
      return HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE;
    }
  }
}