using JetBrains.Annotations;
using JetBrains.Application.UI.Icons.ComposedIcons;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.Icons;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpIconsUtil
  {
    public static IconId GetIconId(this FSharpSymbol symbol)
    {
      var entity = symbol as FSharpEntity;
      if (entity != null)
        return AddAccessibility(GetIconId(entity), entity.Accessibility);

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        var mfvIconId = GetIconId(mfv);
        if (!mfv.IsModuleValueOrMember)
          return mfvIconId;

        var staticIconId = !mfv.IsInstanceMember
          ? PsiSymbolsThemedIcons.ModifiersStatic.Id
          : null;

        var iconId = staticIconId != null
          ? CompositeIconId.Compose(mfvIconId, staticIconId)
          : mfvIconId;

        return AddAccessibility(iconId, mfv.Accessibility);
      }

      if (symbol is FSharpField)
        return PsiSymbolsThemedIcons.Field.Id;

      if (symbol is FSharpUnionCase)
        return PsiSymbolsThemedIcons.Class.Id;

      if (symbol is FSharpParameter)
        return PsiSymbolsThemedIcons.Parameter.Id;

      if (symbol is FSharpGenericParameter)
        return PsiSymbolsThemedIcons.Typeparameter.Id;

      return PsiSymbolsThemedIcons.Variable.Id;
    }

    private static IconId GetIconId(FSharpEntity entity)
    {
      if (entity.IsClass || entity.IsFSharpAbbreviation || entity.IsFSharpRecord || entity.IsFSharpUnion)
        return PsiSymbolsThemedIcons.Class.Id;

      if (entity.IsFSharpModule)
        return CompositeIconId.Compose(PsiSymbolsThemedIcons.Class.Id, PsiSymbolsThemedIcons.ModifiersStatic.Id);

      if (entity.IsValueType)
        return PsiSymbolsThemedIcons.Struct.Id;

      if (entity.IsNamespace)
        return PsiSymbolsThemedIcons.Namespace.Id;

      if (entity.IsDelegate)
        return PsiSymbolsThemedIcons.Delegate.Id;

      if (entity.IsEnum)
        return PsiSymbolsThemedIcons.Enum.Id;

      return PsiSymbolsThemedIcons.Class.Id;
    }

    [NotNull]
    private static IconId AddAccessibility([NotNull] IconId iconId, [NotNull] FSharpAccessibility accessibility)
    {
      if (accessibility.IsInternal)
        return CompositeIconId.Compose(iconId, PsiSymbolsThemedIcons.ModifiersInternal.Id);

      if (accessibility.IsPrivate)
        return CompositeIconId.Compose(iconId, PsiSymbolsThemedIcons.ModifiersPrivate.Id);

      return iconId;
    }

    [NotNull]
    private static IconId GetIconId(FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsModuleValueOrMember)
      {
        if (mfv.IsProperty)
        {
          var propertyId = PsiSymbolsThemedIcons.Property.Id;
          var modifierId =
            mfv.HasGetterMethod
              ? mfv.HasSetterMethod
                ? PsiSymbolsThemedIcons.ModifiersReadWrite.Id
                : PsiSymbolsThemedIcons.ModifiersRead.Id
              : mfv.HasSetterMethod
                ? PsiSymbolsThemedIcons.ModifiersWrite.Id
                : null;
          return modifierId != null
            ? CompositeIconId.Compose(propertyId, modifierId)
            : propertyId;
        }

        if (mfv.IsMember || mfv.IsValCompiledAsMethod)
          return PsiSymbolsThemedIcons.Method.Id;
      }
      return PsiSymbolsThemedIcons.Variable.Id;
    }
  }
}