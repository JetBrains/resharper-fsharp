namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open System
open System.Collections.Concurrent
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Settings
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


[<SettingsKey(typeof<CodeInspectionSettings>, "F# language settings")>]
type FSharpLanguageProjectSettings =
    { [<SettingsEntry(FSharpLanguageLevel.Latest, "F# language level")>]
      LanguageLevel: FSharpLanguageLevel }


[<SolutionComponent>]
type FSharpLanguageLevelProjectProperty(lifetime, locks, projectPropertiesListener, projectSettings,
        persistentProjectItemProperties, settingsSchema: SettingsSchema, solutionToolset: ISolutionToolset) =
    inherit OverridableLanguageLevelProjectProperty<FSharpLanguageLevel, FSharpLanguageVersion>(lifetime, locks,
        projectPropertiesListener, projectSettings, persistentProjectItemProperties,
        settingsSchema.GetKey<FSharpLanguageProjectSettings>().TryFindEntryByMemberName("LanguageLevel"))

    let compilerPathToLanguageLevels = ConcurrentDictionary<FileSystemPath, VersionMapping>()

    let getFSharpProjectConfiguration (project: IProject) targetFrameworkId =
        project.ProjectProperties.TryGetConfiguration<IFSharpProjectConfiguration>(targetFrameworkId)
    
    let (|Version|) (version: Version) =
        version.Major, version.Minor

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
        VersionMapping(languageLevel, languageLevel)
    
    // todo: more versions
    let getLanguageLevelByCompilerVersion (fscVersion: Version): VersionMapping =
        match fscVersion with
        | Version (10, 1000) -> VersionMapping(FSharpLanguageLevel.FSharp47, FSharpLanguageLevel.FSharp50)
        | Version (11, 0) -> VersionMapping(FSharpLanguageLevel.FSharp50, FSharpLanguageLevel.Preview)
        | _ -> null

    let getCompilerVersion (fscPath: FileSystemPath) =
        let assemblyNameInfo = AssemblyNameReader.GetAssemblyNameRaw(fscPath)
        assemblyNameInfo.Version

    let getLanguageLevelByCompilerNoCache (fscPath: FileSystemPath): VersionMapping =
        if fscPath.IsEmpty then getVersionMappingByToolset () else

        let version = getCompilerVersion fscPath
        match getLanguageLevelByCompilerVersion version with
        | null -> getVersionMappingByToolset ()
        | mapping -> mapping

    let getLanguageLevelByCompiler (fscPath: FileSystemPath): VersionMapping =
        compilerPathToLanguageLevels.GetOrAdd(fscPath, getLanguageLevelByCompilerNoCache)

    let getFscPath (configuration: IFSharpProjectConfiguration): FileSystemPath =
        if isNull configuration then FileSystemPath.Empty else

        let path = configuration.PropertiesCollection.GetPropertyValueSafe(FSharpProperties.DotnetFscCompilerPath)
        FileSystemPath.TryParse(path)

    let convertToLanguageLevel (configuration: IFSharpProjectConfiguration) version =
        match version with
        | FSharpLanguageVersion.FSharp46 -> FSharpLanguageLevel.FSharp46
        | FSharpLanguageVersion.FSharp47 -> FSharpLanguageLevel.FSharp47
        | FSharpLanguageVersion.FSharp50 -> FSharpLanguageLevel.FSharp50
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

    override this.GetOverriddenLanguageLevelFromSettings _ = Nullable()

    override this.IsAvailable(_: FSharpLanguageVersion, _: IProject, _: TargetFrameworkId): bool = failwith "todo"
    override this.TryParseCompilationOption _ = failwith "todo"
    override this.ConvertToCompilationOption _ = failwith "todo"
    override this.SetOverridenLanguageLevelInSettings(_, _) = failwith "todo"
    override this.GetPresentation(_, _, _, _) = failwith "todo"
    override this.GetLatestAvailableLanguageLevel _ = failwith "todo"

[<SolutionFeaturePart>]
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
        member this.LanguageLevelOverrider = failwith "todo"
        member this.LanguageVersionModifier = failwith "todo"
