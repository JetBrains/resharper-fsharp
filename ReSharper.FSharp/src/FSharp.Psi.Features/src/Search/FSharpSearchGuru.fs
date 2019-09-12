namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Finder
open JetBrains.ReSharper.Psi.Modules

type ElementDeclarationFileIndex =
    { FileIndex: int
      Module: IPsiModule }


[<SearchGuru(SearchGuruPerformanceEnum.FastFilterOutByLanguage)>]
type FSharpSearchGuru(fsProjectOptionsProvider: IFSharpProjectOptionsProvider) =
    let getTypeElement (fsElement: IFSharpDeclaredElement) =
        match fsElement with
        | :? ITypeElement as typeElement -> typeElement
        | fsElement -> fsElement.GetContainingType()
    
    interface ISearchGuru with
        member x.IsAvailable(_) = true
        member x.BuzzWordFilter(_, words) = words

        member x.GetElementId(element) =
            match element.As<IFSharpDeclaredElement>() with
            | null -> null
            | fsElement ->

            match getTypeElement fsElement with
            | null -> null
            | typeElement ->

            let sourceFiles = typeElement.GetSourceFiles().ReadOnlyList()
            if sourceFiles.Count = 0 then null else

            let declarationFileIndex =
                sourceFiles
                |> Seq.map (fun sourceFile -> fsProjectOptionsProvider.GetFileIndex(sourceFile))
                |> Seq.min

            if declarationFileIndex = -1 then null else

            { Module = typeElement.Module
              FileIndex = declarationFileIndex } :> _


        member x.CanContainReferences(sourceFile, elementId) =
            let fileIndex = elementId :?> ElementDeclarationFileIndex

            if not (sourceFile.LanguageType.Is<FSharpProjectFileType>()) then true else
            if sourceFile.PsiModule != fileIndex.Module then true else

            let sourceFileIndex = fsProjectOptionsProvider.GetFileIndex(sourceFile)
            sourceFileIndex >= fileIndex.FileIndex
