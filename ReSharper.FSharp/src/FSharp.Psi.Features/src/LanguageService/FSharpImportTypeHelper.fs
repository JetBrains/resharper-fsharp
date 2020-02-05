namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open System.Collections.Generic
open JetBrains.ProjectModel
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Finder
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpImportTypeHelper() =
    interface IImportTypeHelper with
        member x.FindTypeCandidates(reference, importTypeCacheFactory) =
            let reference = reference.As<FSharpSymbolReference>()
            if isNull reference || reference.IsQualified then Seq.empty else

            let context = reference.GetElement()
            let sourceFile = context.GetSourceFile()
            let psiModule = context.GetPsiModule()

            let containingModules = 
                reference.GetElement().ContainingNodes<IModuleLikeDeclaration>().ToEnumerable()
                |> Seq.map (fun decl -> decl.DeclaredElement)
                |> Seq.filter isNotNull
                |> HashSet

            let names = reference.GetAllNames().ResultingList()
            let factory = importTypeCacheFactory.Invoke(context)

            names
            |> Seq.collect factory.Invoke
            |> Seq.filter (fun clrDeclaredElement ->
                let typeElement = clrDeclaredElement.As<ITypeElement>()
                if isNull typeElement then false else

                // todo: enable when singleton property cases are supported
                if typeElement.IsUnionCase() then false else

                // Module accessibility is calculated by the factory for us,
                // we only need to check the order inside the project. 
                if typeElement.Module != psiModule then true else

                let searchGuru = psiModule.GetSolution().GetComponent<FSharpSearchGuru>() :> ISearchGuru
                let elementId = searchGuru.GetElementId(typeElement)
                if not (searchGuru.CanContainReferences(sourceFile, elementId)) then false else

                let moduleToOpen = getModuleToOpen typeElement
                not (containingModules.Contains(moduleToOpen)))
            |> Seq.cast

        member x.ReferenceTargetCanBeType _ = true
        member x.ReferenceTargetIsUnlikelyBeType _ = false


[<Language(typeof<FSharpLanguage>)>]
type FSharpQuickFixUtilComponent() =
    interface IQuickFixUtilComponent with
        member x.BindTo(reference, typeElement, _, _) =
            let reference = reference :?> FSharpSymbolReference
            let context = reference.GetElement()
            let fsFile = context.FSharpFile
            let settings = fsFile.GetSettingsStore()

            let moduleToOpen = getModuleToOpen typeElement

            let nameToOpen =
                let style = DeclaredElementPresenter.QUALIFIED_NAME_PRESENTER
                DeclaredElementPresenter.Format(context.Language, style, moduleToOpen).Text

            if nameToOpen.IsNullOrEmpty() then reference :> _ else

            addOpen (context.GetDocumentStartOffset()) fsFile settings nameToOpen
            reference :> _

        member x.AddImportsForExtensionMethod(reference, _) = reference


// todo: ExtensionMethodImportUtilBase
