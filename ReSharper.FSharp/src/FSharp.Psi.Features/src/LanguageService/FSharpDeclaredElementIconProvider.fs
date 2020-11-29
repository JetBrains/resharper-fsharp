namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open System.Linq
open System.Runtime.InteropServices
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.Resources

[<DeclaredElementIconProvider>]
type FSharpDeclaredElementIconProvider() =
    let privateCase = compose PsiSymbolsThemedIcons.EnumMember.Id PsiSymbolsThemedIcons.ModifiersPrivate.Id
    let internalCase = compose PsiSymbolsThemedIcons.EnumMember.Id PsiSymbolsThemedIcons.ModifiersInternal.Id

    let privateField = compose PsiSymbolsThemedIcons.EnumMember.Id PsiSymbolsThemedIcons.ModifiersPrivate.Id
    let internalField = compose PsiSymbolsThemedIcons.EnumMember.Id PsiSymbolsThemedIcons.ModifiersInternal.Id

    let mutableField = compose PsiSymbolsThemedIcons.Field.Id PsiSymbolsThemedIcons.ModifiersWrite.Id
    let mutablePrivateField = compose mutableField PsiSymbolsThemedIcons.ModifiersPrivate.Id
    let mutableInternalField = compose mutableField PsiSymbolsThemedIcons.ModifiersInternal.Id

    interface IDeclaredElementIconProvider with
        member x.GetImageId(declaredElement, _, [<Out>] canApplyExtensions) =
            canApplyExtensions <- true

            match declaredElement with
            | :? IFSharpModule -> FSharpIcons.FSharpModule.Id

            | :? IUnionCase as unionCase ->
                canApplyExtensions <- false
                match unionCase.RepresentationAccessRights with
                | AccessRights.PRIVATE -> privateCase
                | AccessRights.INTERNAL -> internalCase
                | _ -> PsiSymbolsThemedIcons.EnumMember.Id

            | :? IRecordField as field ->
                canApplyExtensions <- false
                if field.IsMutable then
                    match field.RepresentationAccessRights with
                    | AccessRights.PRIVATE -> mutablePrivateField
                    | AccessRights.INTERNAL -> mutableInternalField
                    | _ -> mutableField
                else
                    match field.RepresentationAccessRights with
                    | AccessRights.PRIVATE -> privateField
                    | AccessRights.INTERNAL -> internalField
                    | _ -> PsiSymbolsThemedIcons.Field.Id

            | :? TypeElement as typeElement when
                    typeElement.PresentationLanguage.Is<FSharpLanguage>() && typeElement.IsUnion() ->
                PsiSymbolsThemedIcons.Enum.Id

            | :? IFSharpFieldProperty as fieldProp ->
                canApplyExtensions <- false
                if fieldProp.IsWritable then mutableField else PsiSymbolsThemedIcons.Field.Id

            | :? IActivePatternCase ->
                PsiSymbolsThemedIcons.EnumMember.Id

            | :? IFSharpProperty as property ->
                canApplyExtensions <- false

                let icon = PsiSymbolsThemedIcons.Property.Id
                let icon =
                    match property.IsReadable, property.IsWritable with
                    | true, true -> compose icon PsiSymbolsThemedIcons.ModifiersReadWrite.Id
                    | true, false -> compose icon PsiSymbolsThemedIcons.ModifiersRead.Id
                    | false, true -> compose icon PsiSymbolsThemedIcons.ModifiersWrite.Id
                    | _ -> icon

                let icon =
                    if property.IsStatic then
                        compose icon PsiSymbolsThemedIcons.ModifiersStatic.Id
                    else icon

                match property.RepresentationAccessRights with
                | AccessRights.PRIVATE -> compose icon PsiSymbolsThemedIcons.ModifiersPrivate.Id
                | AccessRights.INTERNAL -> compose icon PsiSymbolsThemedIcons.ModifiersInternal.Id
                | _ -> icon

            | _ -> null
