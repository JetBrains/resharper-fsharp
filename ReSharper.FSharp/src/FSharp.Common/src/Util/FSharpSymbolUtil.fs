[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Common.Util.FSharpSymbolUtil

open JetBrains.Application.UI.Icons.ComposedIcons
open JetBrains.ReSharper.Psi.Resources
open JetBrains.UI.Icons
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices.Extensions

[<CompiledName("GetReturnType")>]
let getReturnType (symbol: FSharpSymbol) =
    match symbol with
    | :? FSharpMemberOrFunctionOrValue as mfv -> Some mfv.ReturnParameter.Type
    | :? FSharpField as field -> Some field.FieldType
    | :? FSharpParameter as param -> Some param.Type
    | :? FSharpUnionCase as case -> Some case.ReturnType
    | _ -> None

[<CompiledName("TryGetFullCompiledName")>]    
let tryGetFullCompiledName (entity: FSharpEntity) =
    entity.TryGetFullCompiledName()

let addAccessibility (iconId: IconId) (accessibility: FSharpAccessibility) =
    let accessibilityIcon =
        if accessibility.IsInternal then PsiSymbolsThemedIcons.ModifiersInternal.Id else
        if accessibility.IsPrivate then PsiSymbolsThemedIcons.ModifiersPrivate.Id else
        null
    if isNull accessibilityIcon then iconId else CompositeIconId.Compose(iconId, accessibilityIcon)

// todo: provide an option to use F# style icons (e.g. functions, values (with no static modifiers), modules)
let rec getIconId = fun (symbol: FSharpSymbol) ->
    match symbol with
    | :? FSharpEntity as entity ->
        let baseIcon =
            if entity.IsClass || entity.IsFSharpRecord
            then PsiSymbolsThemedIcons.Class.Id else

            if entity.IsFSharpModule
            then CompositeIconId.Compose(PsiSymbolsThemedIcons.Class.Id, PsiSymbolsThemedIcons.ModifiersStatic.Id) else

            if entity.IsInterface then PsiSymbolsThemedIcons.Interface.Id else
            if entity.IsValueType then PsiSymbolsThemedIcons.Struct.Id else
            if entity.IsNamespace then PsiSymbolsThemedIcons.Namespace.Id else
            if entity.IsDelegate then PsiSymbolsThemedIcons.Delegate.Id else
            if entity.IsEnum || entity.IsFSharpUnion then PsiSymbolsThemedIcons.Enum.Id else

            let mutable abbrEntity = if entity.IsFSharpAbbreviation then Some entity else None
            let hasAbbrType (entity: FSharpEntity option) =
                entity.IsSome &&
                entity.Value.IsFSharpAbbreviation &&
                entity.Value.AbbreviatedType.HasTypeDefinition

            seq { while hasAbbrType abbrEntity do
                    abbrEntity <- Some abbrEntity.Value.AbbreviatedType.TypeDefinition
                    yield abbrEntity.Value }
            |> Seq.tryLast
            |> Option.map getIconId
            |> Option.defaultValue PsiSymbolsThemedIcons.Class.Id

        addAccessibility baseIcon entity.Accessibility

    | :? FSharpMemberOrFunctionOrValue as mfv ->
        let baseIcon =
            if not mfv.IsModuleValueOrMember then PsiSymbolsThemedIcons.Variable.Id else

            if mfv.IsEvent then PsiSymbolsThemedIcons.Event.Id else
            if mfv.IsExtensionMember then PsiSymbolsThemedIcons.ExtensionMethod.Id else

            if mfv.IsMember then
                if mfv.IsProperty then PsiSymbolsThemedIcons.Property.Id else
                if mfv.IsConstructor then PsiSymbolsThemedIcons.Constructor.Id else
                if mfv.IsExtensionMember then
                    PsiSymbolsThemedIcons.ExtensionMethod.Id else
                PsiSymbolsThemedIcons.Method.Id

            else if mfv.IsValCompiledAsMethod then PsiSymbolsThemedIcons.Method.Id else
            PsiSymbolsThemedIcons.Variable.Id

        let iconId =
            let readWriteModifier =
                if mfv.IsMutable || mfv.HasSetterMethod || mfv.IsRefCell then
                    if mfv.HasGetterMethod then PsiSymbolsThemedIcons.ModifiersReadWrite.Id
                    else PsiSymbolsThemedIcons.ModifiersWrite.Id
                else if mfv.HasGetterMethod then PsiSymbolsThemedIcons.ModifiersRead.Id
                else null

            if isNull readWriteModifier then baseIcon
            else CompositeIconId.Compose(baseIcon, readWriteModifier)

        let iconId =
            if not mfv.IsModuleValueOrMember || mfv.IsInstanceMember || mfv.IsInstanceMemberInCompiledCode || mfv.IsConstructor then iconId
            else CompositeIconId.Compose(iconId, PsiSymbolsThemedIcons.ModifiersStatic.Id)

        addAccessibility iconId mfv.Accessibility

    | :? FSharpField as field ->
        let iconId =
            if field.DeclaringEntity.IsEnum then PsiSymbolsThemedIcons.EnumMember.Id else

            let iconId = PsiSymbolsThemedIcons.Field.Id
            if field.IsStatic then CompositeIconId.Compose(iconId, PsiSymbolsThemedIcons.ModifiersStatic.Id) else iconId
        addAccessibility iconId field.Accessibility

    | :? FSharpUnionCase as unionCase ->
        let iconId =  addAccessibility PsiSymbolsThemedIcons.EnumMember.Id unionCase.Accessibility
        CompositeIconId.Compose(iconId, PsiSymbolsThemedIcons.ModifiersStatic.Id)
    | :? FSharpParameter -> PsiSymbolsThemedIcons.Parameter.Id
    | :? FSharpGenericParameter
    | :? FSharpStaticParameter -> PsiSymbolsThemedIcons.Typeparameter.Id
    | :? FSharpActivePatternCase -> PsiSymbolsThemedIcons.Method.Id
    | _ -> null
