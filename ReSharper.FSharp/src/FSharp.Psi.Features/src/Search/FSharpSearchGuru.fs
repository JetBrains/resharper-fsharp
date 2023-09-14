namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Search

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Finder
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Rider.Backend.Env

type FSharpSearchGuruElementId =
    { DeclaredElement: IDeclaredElement
      PsiModule: IPsiModule
      FileIndex: int }


[<SearchGuru(SearchGuruPerformanceEnum.FastFilterOutByLanguage)>]
[<ZoneMarker(typeof<IResharperHostCoreFeatureZone>, typeof<IRiderFeatureEnvironmentZone>)>]
type FSharpSearchGuru(fsProjectOptionsProvider: IFcsProjectProvider) =
    let getTypeElement (fsElement: IFSharpDeclaredElement) =
        match fsElement with
        | :? ITypeElement as typeElement -> typeElement
        | fsElement -> fsElement.GetContainingType()

    interface ISearchGuru with
        member x.IsAvailable _ = true
        member x.BuzzWordFilter(_, words) = words

        member x.GetElementId(element) =
            let fsDeclaredElement = element.As<IFSharpDeclaredElement>()
            if isNull fsDeclaredElement then null else

            let typeElement = getTypeElement fsDeclaredElement
            if isNull typeElement then null else

            let sourceFiles = typeElement.GetSourceFiles()
            if sourceFiles.IsEmpty then null else

            let declarationFileIndex =
                sourceFiles.ReadOnlyList()
                |> Seq.map fsProjectOptionsProvider.GetFileIndex
                |> Seq.min

            if declarationFileIndex = -1 then null else

            { DeclaredElement = element
              PsiModule = fsDeclaredElement.Module
              FileIndex = declarationFileIndex } :> _


        member x.CanContainReferences(sourceFile, elementId) =
            if not (sourceFile.LanguageType.Is<FSharpProjectFileType>()) then true else

            let fsElementId = elementId :?> FSharpSearchGuruElementId

            let typePrivateMember = fsElementId.DeclaredElement.As<ITypePrivateMember>()
            if isNotNull typePrivateMember then
                typePrivateMember.GetSourceFiles().Contains(sourceFile) else

            if sourceFile.PsiModule != fsElementId.PsiModule then true else

            let sourceFileIndex = fsProjectOptionsProvider.GetFileIndex(sourceFile)
            sourceFileIndex >= fsElementId.FileIndex
