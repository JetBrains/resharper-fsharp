namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open System.Runtime.InteropServices
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.Resources

[<DeclaredElementIconProvider>]
type FSharpDeclaredElementIconProvider() =
    static let privateCase = compose PsiSymbolsThemedIcons.EnumMember.Id PsiSymbolsThemedIcons.ModifiersPrivate.Id
    static let internalCase = compose PsiSymbolsThemedIcons.EnumMember.Id PsiSymbolsThemedIcons.ModifiersInternal.Id
    static let mutableField = compose PsiSymbolsThemedIcons.Field.Id PsiSymbolsThemedIcons.ModifiersWrite.Id

    interface IDeclaredElementIconProvider with
        member x.GetImageId(declaredElement, languageType, [<Out>] canApplyExtensions) =
            canApplyExtensions <- true
            if not (languageType.Is<FSharpLanguage>()) then null else

            match declaredElement with
            | :? IModule -> FSharpIcons.FSharpModule.Id

            | :? IUnionCase as unionCase ->
                canApplyExtensions <- false
                match unionCase.RepresentationAccessRights with
                | AccessRights.PRIVATE -> privateCase
                | AccessRights.INTERNAL -> internalCase
                | _ -> PsiSymbolsThemedIcons.EnumMember.Id

            | :? TypeElement as typeElement when typeElement.IsUnion() ->
                PsiSymbolsThemedIcons.Enum.Id

            | :? IFSharpFieldProperty as fieldProp ->
                canApplyExtensions <- false
                if fieldProp.IsWritable then mutableField else PsiSymbolsThemedIcons.Field.Id

            | _ -> null
