namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open System
open System.Collections.Concurrent
open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Parts
open JetBrains.Diagnostics
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties
open JetBrains.RdBackend.Common.Env
open JetBrains.RdBackend.Common.Features.ProjectModel.View.EditProperties.Projects.MsBuild.Extensions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds

[<AllowNullLiteral>]
type VersionMapping(defaultVersion: FSharpLanguageLevel, latestMajor: FSharpLanguageLevel,
        latestMinor: FSharpLanguageLevel, preview: FSharpLanguageLevel) =

    new (latestMajor, latestMinor, preview) =
        VersionMapping(latestMinor, latestMajor, latestMinor, preview)

    new (latestMinor, preview) =
        VersionMapping(latestMinor, latestMinor, latestMinor, preview)

    member this.DefaultVersion = defaultVersion
    member this.LatestMajor = latestMajor
    member this.LatestMinor = latestMinor
    member this.Preview = preview


[<SolutionComponent(Instantiation.ContainerAsyncPrimaryThread (* PrimaryThread due to not thread safe ProjectSettingsStorageComponent *) )>]
type FSharpLanguageLevelProjectProperty(lifetime, locks, projectPropertiesListener, solutionToolset: ISolutionToolset) =
    inherit LanguageLevelProjectProperty<FSharpLanguageLevel, FSharpLanguageVersion>(
        lifetime, locks, projectPropertiesListener)

    let compilerPathToLanguageLevels = ConcurrentDictionary<VirtualFileSystemPath, VersionMapping>()

    let getFSharpProjectConfiguration (project: IProject) targetFrameworkId =
        project.ProjectProperties.TryGetConfiguration<IFSharpProjectConfiguration>(targetFrameworkId)

    let (|Version|) (version: Version) =
        version.Major, version.Minor, version.Build

    // TODO: more versions
    let getLanguageLevelByToolsetVersion () =
        match solutionToolset.GetBuildTool() with
        | null -> FSharpLanguageLevel.Latest
        | toolset ->

        match int toolset.Version.Major with
        | 14 -> FSharpLanguageLevel.FSharp45
        | 15 -> FSharpLanguageLevel.FSharp47
        | 16 -> FSharpLanguageLevel.FSharp50
        | _ -> FSharpLanguageLevel.Latest

    let getVersionMappingByToolset () =
        let languageLevel = getLanguageLevelByToolsetVersion ()
        VersionMapping(languageLevel, FSharpLanguageLevel.Preview)

    let getLanguageLevelByCompilerVersion (fscVersion: Version): VersionMapping =
        match fscVersion with
        | Version (10, 1000, _) -> VersionMapping(FSharpLanguageLevel.FSharp47, FSharpLanguageLevel.FSharp50)
        | Version (11, 0, _) -> VersionMapping(FSharpLanguageLevel.FSharp50, FSharpLanguageLevel.FSharp60)
        | Version (12, minor, build) ->
            if minor < 4 then VersionMapping(FSharpLanguageLevel.FSharp60, FSharpLanguageLevel.FSharp70)
            elif minor >= 4 && minor <= 7 then VersionMapping(FSharpLanguageLevel.FSharp70, FSharpLanguageLevel.FSharp80)
            else
                match minor with
                | 8 when build < 200 -> VersionMapping(FSharpLanguageLevel.FSharp80, FSharpLanguageLevel.FSharp90)
                | 8 -> VersionMapping(FSharpLanguageLevel.FSharp81, FSharpLanguageLevel.FSharp90)
                | 9 -> VersionMapping(FSharpLanguageLevel.FSharp90, FSharpLanguageLevel.FSharp100)
                | _ -> VersionMapping(FSharpLanguageLevel.FSharp100, FSharpLanguageLevel.Preview)
        | _ -> null

    let getCompilerVersion (fscPath: VirtualFileSystemPath) =
        let assemblyNameInfo = AssemblyNameReader.TryReadAssemblyNameFromFile(fscPath)
        assemblyNameInfo.Version

    let getLanguageLevelByCompilerNoCache (fscPath: VirtualFileSystemPath): VersionMapping =
        if not fscPath.ExistsFile then getVersionMappingByToolset () else

        let version = getCompilerVersion fscPath
        match getLanguageLevelByCompilerVersion version with
        | null -> getVersionMappingByToolset ()
        | mapping -> mapping

    let getLanguageLevelByCompiler (fscPath: VirtualFileSystemPath): VersionMapping =
        compilerPathToLanguageLevels.GetOrAdd(fscPath, getLanguageLevelByCompilerNoCache)

    let getFscPath (configuration: IFSharpProjectConfiguration): VirtualFileSystemPath =
        if isNull configuration then VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext) else

        match configuration.PropertiesCollection.TryGetValue(FSharpProperties.DotnetFscCompilerPath) with
        | true, path -> path.Trim('"').ParseVirtualPathSafe(InteractionContext.SolutionContext)
        | false, _ -> VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext)

    let convertToLanguageLevel (configuration: IFSharpProjectConfiguration) version =
        match version with
        | FSharpLanguageVersion.FSharp46 -> FSharpLanguageLevel.FSharp46
        | FSharpLanguageVersion.FSharp47 -> FSharpLanguageLevel.FSharp47
        | FSharpLanguageVersion.FSharp50 -> FSharpLanguageLevel.FSharp50
        | FSharpLanguageVersion.FSharp60 -> FSharpLanguageLevel.FSharp60
        | FSharpLanguageVersion.FSharp70 -> FSharpLanguageLevel.FSharp70
        | FSharpLanguageVersion.FSharp80 ->
            let levelByCompiler = getFscPath configuration |> getLanguageLevelByCompiler
            if isNull levelByCompiler || levelByCompiler.LatestMinor <= FSharpLanguageLevel.FSharp80 then
                FSharpLanguageLevel.FSharp80
            else
                FSharpLanguageLevel.FSharp81

        | FSharpLanguageVersion.FSharp90 -> FSharpLanguageLevel.FSharp90
        | FSharpLanguageVersion.FSharp100 -> FSharpLanguageLevel.FSharp100
        | FSharpLanguageVersion.Default -> (getFscPath configuration |> getLanguageLevelByCompiler).DefaultVersion
        | FSharpLanguageVersion.LatestMajor -> (getFscPath configuration |> getLanguageLevelByCompiler).LatestMajor
        | FSharpLanguageVersion.Latest -> (getFscPath configuration |> getLanguageLevelByCompiler).LatestMinor
        | FSharpLanguageVersion.Preview -> (getFscPath configuration |> getLanguageLevelByCompiler).Preview
        | _ -> getLanguageLevelByToolsetVersion ()

    let getLatestAvailableLanguageLevel project targetFramework =
        match getFSharpProjectConfiguration project targetFramework with
        | null -> getLanguageLevelByToolsetVersion ()
        | configuration -> convertToLanguageLevel configuration FSharpLanguageVersion.Preview

    override this.LanguageName = FSharpLanguage.Instance.PresentableName

    override this.IsApplicableToProject(project) =
        project.ProjectProperties :? FSharpProjectProperties

    override this.GetDefaultLanguageLevel(project, targetFrameworkId) =
        match getFSharpProjectConfiguration project targetFrameworkId with
        | null -> getLanguageLevelByToolsetVersion ()
        | configuration -> convertToLanguageLevel configuration configuration.LanguageVersion

    override this.GetLanguageVersion(project, targetFrameworkId) =
        match getFSharpProjectConfiguration project targetFrameworkId with
        | null -> FSharpLanguageVersion.Default
        | configuration -> configuration.LanguageVersion

    override this.IsAvailable(languageLevel: FSharpLanguageLevel, project, targetFrameworkId) =
        let latestAvailableLanguageLevel = getLatestAvailableLanguageLevel project targetFrameworkId
        languageLevel <= latestAvailableLanguageLevel

    override this.GetLatestAvailableLanguageLevel(project, targetFrameworkId) =
        getLatestAvailableLanguageLevel project targetFrameworkId

    override this.ConvertToLanguageLevel(languageVersion, project, targetFrameworkId) =
        match getFSharpProjectConfiguration project targetFrameworkId with
        | null -> getLanguageLevelByToolsetVersion ()
        | configuration -> convertToLanguageLevel configuration languageVersion

    override this.ConvertToLanguageVersion(languageLevel) = FSharpLanguageLevel.toLanguageVersion languageLevel

    override this.IsAvailable(languageVersion: FSharpLanguageVersion, project: IProject,
            targetFrameworkId: TargetFrameworkId) =
        if languageVersion = FSharpLanguageVersion.Default then true else

        let latestAvailableLanguageLevel = this.GetLatestAvailableLanguageLevel(project, targetFrameworkId)

        match languageVersion with
        | FSharpLanguageVersion.Latest
        | FSharpLanguageVersion.LatestMajor
        | FSharpLanguageVersion.Preview -> latestAvailableLanguageLevel >= FSharpLanguageLevel.FSharp47
        | _ ->

        let languageLevel = this.ConvertToLanguageLevel(languageVersion, project, targetFrameworkId)
        languageLevel <= latestAvailableLanguageLevel

    override this.TryParseCompilationOption(version) =
        FSharpLanguageVersion.tryParseCompilationOption FSharpLanguageVersion.Default version
        |> Option.toNullable

    override this.ConvertToCompilationOption(version: FSharpLanguageVersion) =
        FSharpLanguageVersion.toCompilerOptionValue version

    override this.GetPresentation(version, _, _, _) =
        FSharpLanguageVersion.toString version

    override this.GetLatestAvailableLanguageLevelImpl(_, _) = failwith "todo"

    override this.LanguageLevelComparer = FSharpLanguageLevelComparer.Instance :> _

    interface ISolutionToolsetListener with
        member this.Changed _ =
            compilerPathToLanguageLevels.Clear()


[<SolutionFeaturePart(InstantiationEx.LegacyDefault)>]
type FSharpLanguageLevelProvider(projectProperty: FSharpLanguageLevelProjectProperty) =
    let (|PsiModule|) (psiModule: IPsiModule) =
        let project = psiModule.ContainingProjectModule.NotNull() :?> IProject
        project, psiModule.TargetFrameworkId

    interface ILanguageLevelProvider<FSharpLanguageLevel, FSharpLanguageVersion> with
        member this.IsApplicable(psiModule) =
            psiModule.ContainingProjectModule :? IProject

        member this.GetLanguageLevel(PsiModule(project, targetFramework)) =
            projectProperty.GetLanguageLevel(project, targetFramework)

        member this.ConvertToLanguageLevel(languageVersion, PsiModule(project, targetFramework)) =
            projectProperty.ConvertToLanguageLevel(languageVersion, project, targetFramework)

        member this.ConvertToLanguageVersion(languageLevel) =
            FSharpLanguageLevel.toLanguageVersion languageLevel

        member this.IsAvailable(languageLevel: FSharpLanguageLevel, PsiModule(project, targetFramework)): bool =
            languageLevel <= projectProperty.GetLatestAvailableLanguageLevel(project, targetFramework)

        member this.TryGetLanguageVersion(PsiModule(project, targetFramework)) =
            Nullable(projectProperty.GetLanguageVersion(project, targetFramework))

        member this.IsAvailable(_: FSharpLanguageVersion, _: IPsiModule): bool = failwith "todo"
        member this.GetLatestAvailableLanguageLevel _ = failwith "todo"
        member this.LanguageVersionModifier = failwith "todo"

[<ShellFeaturePart>]
[<ZoneMarker(typeof<IReSharperHostNetSharedFeatureZone>)>]
type FSharpLanguageSpecificItemsProvider() =
    inherit LanguageSpecificItemsProviderBase<FSharpLanguageVersion, FSharpLanguageLevel>()

    static let specialVersions =
        [| FSharpLanguageVersion.Default
           FSharpLanguageVersion.LatestMajor
           FSharpLanguageVersion.Latest
           FSharpLanguageVersion.Preview |]

    override this.IsApplicable(project: IProject) =
        project.ProjectProperties :? FSharpProjectProperties && base.IsApplicable(project)

    override this.GetAvailableLanguageVersions(projectProperty, project, targetFrameworkId) =
        EnumEx.GetValues<FSharpLanguageVersion>()
        |> Seq.filter (fun version -> not (Array.contains version specialVersions))
        |> Seq.filter (fun version -> projectProperty.IsAvailable(version, project, targetFrameworkId))
        |> Seq.sortDescending
        |> Seq.append specialVersions
        |> Seq.toList :> _
