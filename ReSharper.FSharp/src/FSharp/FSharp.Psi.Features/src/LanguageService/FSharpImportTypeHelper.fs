namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.ProjectModel
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Searching
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Finder
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree

[<Language(typeof<FSharpLanguage>)>]
type FSharpImportTypeHelper() =
    let [<Literal>] opName = "FSharpImportTypeHelper.FindTypeCandidates"

    let isApplicable (context: IFSharpReferenceOwner) =
        let referenceName = context.As<ITypeReferenceName>()
        not (isNotNull (OpenStatementNavigator.GetByReferenceName(referenceName)))

    interface IImportTypeHelper with
        member x.FindTypeCandidates(reference, importTypeCacheFactory) =
            let reference = reference.As<FSharpSymbolReference>()
            if isNull reference || reference.IsQualified then Seq.empty else

            let context = reference.GetElement()
            if not (isApplicable context) then Seq.empty else

            let sourceFile = context.GetSourceFile()
            let psiModule = context.GetPsiModule()
            let containingModules = getContainingModules context
            let referenceStartOffset = context.GetDocumentStartOffset()

            let fsFile = sourceFile.FSharpFile
            let settings = fsFile.GetSettingsStoreWithEditorConfig()

            let names = reference.GetAllNames().ResultingList()
            let factory = importTypeCacheFactory.Invoke(context)

            let canReferenceInsideProject typeElement =
                let searchFilter = psiModule.GetSolution().GetComponent<FSharpSearchFilter>() :> ISearchFilter
                let elementKey = searchFilter.TryGetKey(typeElement)
                searchFilter.CanContainReferences(sourceFile, elementKey)

            let fsAssemblyAutoOpenCache = psiModule.GetSolution().GetComponent<FSharpAutoOpenCache>()

            let mutable candidates: ITypeElement seq =
                names
                |> Seq.collect factory.Invoke
                |> Seq.filter (fun clrDeclaredElement ->
                    let typeElement = clrDeclaredElement.As<ITypeElement>()
                    if isNull typeElement then false else

                    // todo: enable when singleton property cases are supported
                    if typeElement.IsUnionCase() then false else

                    if typeElement.Module == psiModule &&
                            not (canReferenceInsideProject typeElement) then false else

                    let moduleToOpen = getModuleToOpen typeElement
                    if containingModules.Contains(moduleToOpen) then false else

                    if typeElement.Module != psiModule &&
                            not (psiModule.References(typeElement.Module)) then
                        true else

                    let moduleToImport = ModuleToImport.DeclaredElement(moduleToOpen)
                    let moduleDecl, _ = findModuleToInsertTo fsFile referenceStartOffset settings moduleToImport
                    let qualifiedElementList = moduleToImport.GetQualifiedElementList(moduleDecl, true)
                    let names = qualifiedElementList |> List.map (fun el -> el.GetSourceName())

                    let autoOpenedModules = fsAssemblyAutoOpenCache.GetAutoOpenedModules(typeElement.Module)
                    if autoOpenedModules.Count > 0 && autoOpenedModules.Contains(String.concat "." names) then false else

                    let fsModule = typeElement.As<IFSharpModule>()
                    if isNotNull fsModule && isNotNull fsModule.AssociatedTypeElement then false else

                    let symbolUse = fsFile.CheckerService.ResolveNameAtLocation(context, names, false, opName)
                    Option.isSome symbolUse)
                |> Seq.cast


            let referenceName = context.As<ITypeArgumentOwner>()
            if isNotNull referenceName then
                let typeArgumentList = referenceName.TypeArgumentList
                if isNull typeArgumentList || typeArgumentList.TypeUsages.Count = 0 then () else

                let typesCount = typeArgumentList.TypeUsages.Count
                candidates <- candidates |> Seq.filter (fun c -> c.TypeParameters.Count = typesCount)

            let typeReferenceName = context.As<ITypeReferenceName>()
            if isNotNull (AttributeNavigator.GetByReferenceName(typeReferenceName)) then
                let attributeTypeElement = context.GetPredefinedType().Attribute.GetTypeElement()
                candidates <- candidates |> Seq.filter (fun c -> c.IsDescendantOf(attributeTypeElement))

            if isNotNull (InheritMemberNavigator.GetByTypeName(typeReferenceName)) then
                candidates <- candidates |> Seq.filter (fun c -> c :? IInterface || c :? IClass || c :? IStruct)

            candidates

        member x.ReferenceTargetCanBeType _ = true
        member x.ReferenceTargetIsUnlikelyBeType _ = false





// todo: ExtensionMethodImportUtilBase
