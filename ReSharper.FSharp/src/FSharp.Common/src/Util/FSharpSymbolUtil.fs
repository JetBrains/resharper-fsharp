[<AutoOpen; Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Common.Util.FSharpSymbolUtil

open JetBrains.Application.UI.Icons.ComposedIcons
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Psi.Resources
open JetBrains.UI.Icons
open JetBrains.Util
open JetBrains.Util.Logging
open Microsoft.FSharp.Compiler.SourceCodeServices

[<Extension; CompiledName("IsRefCell")>]
let isRefCell (mfv: FSharpMemberOrFunctionOrValue) =
    try mfv.IsRefCell
    with e ->
        Logger.LogMessage(LoggingLevel.WARN, "FSharpSymbolUtil.isRefCell fail: {0}", mfv)
        Logger.LogExceptionSilently(e)
        false

[<Extension; CompiledName("IsValCompiledAsMethod")>]
let isValCompiledAsMethod (mfv: FSharpMemberOrFunctionOrValue) =
    try mfv.IsValCompiledAsMethod
    with e ->
        Logger.LogMessage(LoggingLevel.WARN, "FSharpSymbolUtil.isValCompiledAsMethod fail: {0}", mfv)
        Logger.LogExceptionSilently(e)
        false


[<CompiledName("GetReturnType")>]
let getReturnType (symbol: FSharpSymbol) =
    match symbol with
    | :? FSharpMemberOrFunctionOrValue as mfv -> Some mfv.ReturnParameter.Type
    | :? FSharpField as field -> Some field.FieldType
    | :? FSharpParameter as param -> Some param.Type
    | :? FSharpUnionCase as case -> Some case.ReturnType
    | _ -> None

let compose a b = CompositeIconId.Compose(a, b)

let addAccessibility (iconId: IconId) (accessibility: FSharpAccessibility) =
    if accessibility.IsPublic then iconId else
    if accessibility.IsInternal then compose iconId PsiSymbolsThemedIcons.ModifiersInternal.Id else
    if accessibility.IsPrivate then compose iconId PsiSymbolsThemedIcons.ModifiersPrivate.Id else
    compose iconId PsiSymbolsThemedIcons.ModifiersProtected.Id

let staticProperty = compose PsiSymbolsThemedIcons.Property.Id PsiSymbolsThemedIcons.ModifiersStatic.Id
let staticMethod   = compose PsiSymbolsThemedIcons.Method.Id   PsiSymbolsThemedIcons.ModifiersStatic.Id

let rec getIconId (symbol: FSharpSymbol) =
    match symbol with
    | :? FSharpEntity as entity ->
        if entity.IsNamespace then PsiSymbolsThemedIcons.Namespace.Id else

        let baseIcon =
            if entity.IsClass || entity.IsFSharpRecord
            then PsiSymbolsThemedIcons.Class.Id else

            if entity.IsFSharpModule then FSharpIcons.FSharpModule.Id else

            if entity.IsInterface then PsiSymbolsThemedIcons.Interface.Id else
            if entity.IsValueType then PsiSymbolsThemedIcons.Struct.Id else
            if entity.IsFSharpUnion || entity.IsEnum then PsiSymbolsThemedIcons.Enum.Id else
            if entity.IsDelegate then PsiSymbolsThemedIcons.Delegate.Id else

            let isTypeAbbr (entity: FSharpEntity) =
                entity.IsFSharpAbbreviation && entity.AbbreviatedType.HasTypeDefinition

            if not (isTypeAbbr entity) then PsiSymbolsThemedIcons.Class.Id else

            let mutable abbrEntity = entity
            while isTypeAbbr abbrEntity do
                abbrEntity <- abbrEntity.AbbreviatedType.TypeDefinition

            getIconId abbrEntity

        addAccessibility baseIcon entity.Accessibility

    | :? FSharpMemberOrFunctionOrValue as mfv ->
        let accessibility = mfv.Accessibility
        let isModuleValueOrMember = mfv.IsModuleValueOrMember
        let isMember = mfv.IsMember
        let isProperty = mfv.IsProperty
        let isCtor = mfv.IsConstructor

        if isModuleValueOrMember && not isMember && accessibility.IsPublic then
            if isValCompiledAsMethod mfv then staticMethod else staticProperty
        else

        let baseIcon =
            if not isModuleValueOrMember then PsiSymbolsThemedIcons.Variable.Id else

            if mfv.IsEvent then PsiSymbolsThemedIcons.Event.Id else
            if mfv.IsExtensionMember then PsiSymbolsThemedIcons.ExtensionMethod.Id else

            if isMember then
                if isProperty then PsiSymbolsThemedIcons.Property.Id else
                if isCtor then PsiSymbolsThemedIcons.Constructor.Id else
                PsiSymbolsThemedIcons.Method.Id

            else PsiSymbolsThemedIcons.Variable.Id

        let iconId =
            let readWriteModifier =
                if isProperty then
                    if mfv.HasSetterMethod then
                        if mfv.HasGetterMethod then PsiSymbolsThemedIcons.ModifiersReadWrite.Id else
                        PsiSymbolsThemedIcons.ModifiersWrite.Id
                    else
                        if mfv.HasGetterMethod then PsiSymbolsThemedIcons.ModifiersRead.Id else null

                else if mfv.IsMutable || isRefCell mfv then PsiSymbolsThemedIcons.ModifiersWrite.Id
                else null

            if isNull readWriteModifier then baseIcon
            else compose baseIcon readWriteModifier

        let iconId =
            if not isModuleValueOrMember || isCtor || mfv.IsInstanceMember then iconId
            else CompositeIconId.Compose(iconId, PsiSymbolsThemedIcons.ModifiersStatic.Id)

        addAccessibility iconId accessibility

    | :? FSharpField as field ->
        match field.DeclaringEntity with
        | Some entity when entity.IsEnum -> PsiSymbolsThemedIcons.EnumMember.Id
        | _ ->

        let iconId =
            let iconId = PsiSymbolsThemedIcons.Field.Id
            if field.IsStatic then compose iconId PsiSymbolsThemedIcons.ModifiersStatic.Id else iconId
        addAccessibility iconId field.Accessibility

    | :? FSharpUnionCase -> PsiSymbolsThemedIcons.EnumMember.Id
    | :? FSharpParameter -> PsiSymbolsThemedIcons.Parameter.Id
    | :? FSharpActivePatternCase -> PsiSymbolsThemedIcons.Method.Id
    | :? FSharpGenericParameter
    | :? FSharpStaticParameter -> PsiSymbolsThemedIcons.Typeparameter.Id
    | _ -> null
