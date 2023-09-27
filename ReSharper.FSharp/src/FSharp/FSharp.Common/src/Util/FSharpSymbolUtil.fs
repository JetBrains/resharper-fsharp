[<AutoOpen; Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.FSharpSymbolUtil

open FSharp.Compiler.Symbols
open JetBrains.Application.UI.Icons.ComposedIcons
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Psi.Resources
open JetBrains.UI.Icons
open JetBrains.Util.Logging

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

let isEnumMember (field: FSharpField) =
    match field.DeclaringEntity with
    | Some entity -> entity.IsEnum
    | _ -> false

[<CompiledName("GetReturnType")>]
let getReturnType (symbol: FSharpSymbol) =
    match symbol with
    | :? FSharpMemberOrFunctionOrValue as mfv -> Some mfv.ReturnParameter.Type
    | :? FSharpField as field -> Some field.FieldType
    | :? FSharpParameter as param -> Some param.Type
    | :? FSharpUnionCase as case -> Some case.ReturnType
    | _ -> None

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


[<Extension; CompiledName("PatternName")>]
let patternName (pattern: FSharpActivePatternGroup) =
    match pattern.Name with
    | Some name -> name
    | _ ->

    let joinedNames = String.concat "|" pattern.Names
    let wildCase = if pattern.IsTotal then "|" else "_|"
    "|" + joinedNames + wildCase

[<Extension; CompiledName("GetAbbreviatedEntity")>]
let rec getAbbreviatedEntity (entity: FSharpEntity) =
    if entity.IsFSharpAbbreviation && entity.AbbreviatedType.HasTypeDefinition then
        getAbbreviatedEntity entity.AbbreviatedType.TypeDefinition
    else
        entity

[<Extension; CompiledName("GetAbbreviatedType")>]
let rec getAbbreviatedType (fcsType: FSharpType) =
    if isNotNull fcsType && fcsType.IsAbbreviation then
        getAbbreviatedType fcsType.AbbreviatedType
    else
        fcsType

let tryGetAbbreviatedTypeEntity (fcsType: FSharpType) =
    let abbreviatedType = getAbbreviatedType fcsType
    if abbreviatedType.HasTypeDefinition then
        Some abbreviatedType.TypeDefinition
    else
        None

[<Extension; CompiledName("HasMeasureParameter")>]
let hasMeasureParameter(entity: FSharpEntity) =
    entity.GenericParameters.Count > 0 && entity.GenericParameters[0].IsMeasure;

type FSharpActivePatternGroup with
    member x.PatternName = patternName x


type FcsEntityInstance =
    { Entity: FSharpEntity
      Substitution: (FSharpGenericParameter * FSharpType) list }

    member this.FcsType =
        this.Entity.AsType().Instantiate(this.Substitution)

    override x.ToString() = x.Entity.ToString()

module FcsEntityInstance =
    let create fcsType =
        let fcsType = getAbbreviatedType fcsType
        if isNull fcsType || not fcsType.HasTypeDefinition then Unchecked.defaultof<_> else

        let fcsEntity = fcsType.TypeDefinition
        let substitution = Seq.zip fcsEntity.GenericParameters fcsType.GenericArguments |> Seq.toList

        { Entity = fcsEntity
          Substitution = substitution }


type FcsMfvInstance =
    { Mfv: FSharpMemberOrFunctionOrValue
      DisplayContext: FSharpDisplayContext
      Substitution: (FSharpGenericParameter * FSharpType) list }

    override x.ToString() = x.Mfv.ToString()

module FcsMfvInstance =
    let create mfv displayContext substitution =
        { Mfv = mfv
          Substitution = substitution
          DisplayContext = displayContext }
