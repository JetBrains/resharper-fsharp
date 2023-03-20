namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Collections.Generic
open FSharp.Compiler.CodeAnalysis
open JetBrains.Application
open JetBrains.Application.FileSystemTracker
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.MSBuild
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Strategies
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds

[<ShellComponent>]
type FSharpTargetsProjectLoadModificator() =
    let fsTargets =
        [| "GenerateCode"
           "GenerateFSharpInternalsVisibleToFile"
           "GenerateAssemblyFileVersionTask"
           "ImplicitlyExpandNETStandardFacades" |]

    interface MsBuildLegacyLoadStrategy.IModificator with
        member x.IsApplicable(mark) =
            match mark with
            | FSharpProjectMark -> true
            | _ -> false

        member x.Modify(targets) =
            targets.AddRange(fsTargets)


module FcsProjectBuilder =
    let itemsDelimiters = [| ';'; ','; ' ' |]

    let splitAndTrim (delimiters: char[]) (s: string) =
        if isNull s then EmptyArray.Instance else
        s.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)

    let getProjectConfiguration (targetFramework: TargetFrameworkId) (project: IProject) =
        let projectProperties = project.ProjectProperties
        projectProperties.ActiveConfigurations.TryGetConfiguration(targetFramework).As<IManagedProjectConfiguration>()

    let getDefines (configuration: IManagedProjectConfiguration) =
        if isNull configuration then [] else

        splitAndTrim itemsDelimiters configuration.DefineConstants
        |> List.ofArray

[<SolutionComponent>]
type FcsProjectBuilder(lifetime: Lifetime, checkerService: FcsCheckerService, itemsContainer: IFSharpItemsContainer,
        modulePathProvider: ModulePathProvider, fileSystemTracker: IFileSystemTracker, logger: ILogger) =

    let mutable stamp = 0L

    let getNextStamp () =
        let result = stamp
        stamp <- stamp + 1L
        result

    let defaultOptions =
        [| "--noframework"
           "--debug:full"
           "--debug+"
           "--optimize-"
           "--tailcalls-"
           "--fullpaths"
           "--flaterrors"
           "--highentropyva+"
           "--noconditionalerasure"
           "--ignorelinedirectives" |]

    let unusedValuesWarns =
        [| "--warnon:1182" |]

    let xmlDocsNoWarns =
        [| "--nowarn:3390" |]

    let getOutputType (outputType: ProjectOutputType) =
        match outputType with
        | ProjectOutputType.CONSOLE_EXE -> "exe"
        | ProjectOutputType.WIN_EXE -> "winexe"
        | ProjectOutputType.MODULE -> "module"
        | _ -> "library"

    abstract GetProjectItemsPaths:
        project: IProject * targetFrameworkId: TargetFrameworkId -> (VirtualFileSystemPath * BuildAction)[]

    default x.GetProjectItemsPaths(project, targetFrameworkId) =
        let projectMark = project.GetProjectMark().NotNull()
        itemsContainer.GetProjectItemsPaths(projectMark, targetFrameworkId)

    member x.GetProjectFilesAndResources(project: IProject, targetFrameworkId) =
        let sourceFiles = List()
        let resources = List()

        let sigFiles = HashSet()
        let implsWithSigs = HashSet()

        let projectItems = x.GetProjectItemsPaths(project, targetFrameworkId)

        for path, buildAction in projectItems do
            match buildAction with
            | SourceFile ->
                sourceFiles.Add(path)
                let fileName = path.NameWithoutExtension
                match path.ExtensionNoDot with
                | SigExtension -> sigFiles.Add(fileName) |> ignore
                | ImplExtension when sigFiles.Contains(fileName) -> implsWithSigs.add(path)
                | _ -> ()

            | Resource -> resources.Add(path)
            | _ -> ()

        let resources: IList<_> = if resources.IsEmpty() then EmptyList.InstanceList else resources :> _
        let implsWithSigs: ISet<_> = if implsWithSigs.IsEmpty() then EmptySet.Instance :> _ else implsWithSigs :> _

        sourceFiles.ToArray(), implsWithSigs, resources

    member x.BuildFcsProject(psiModule: IPsiModule, project: IProject): FcsProject =
        let targetFrameworkId = psiModule.TargetFrameworkId
        let projectProperties = project.ProjectProperties

        let otherOptions = List()

        let outPath = project.GetOutputFilePath(targetFrameworkId)
        if not outPath.IsEmpty then
            otherOptions.Add("--out:" + outPath.FullPath)

        otherOptions.AddRange(defaultOptions)
        otherOptions.AddRange(unusedValuesWarns)
        otherOptions.AddRange(xmlDocsNoWarns)

        match projectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg ->
            let definedConstants = FcsProjectBuilder.getDefines cfg
            otherOptions.AddRange(definedConstants |> Seq.map (fun c -> "--define:" + c))

            otherOptions.Add($"--target:{getOutputType cfg.OutputType}")

            otherOptions.Add$"--warn:{cfg.WarningLevel}"

            if cfg.TreatWarningsAsErrors then
                otherOptions.Add("--warnaserror")

            if Shell.Instance.IsTestShell then
                let languageLevel = FSharpLanguageLevel.ofPsiModuleNoCache psiModule
                let langVersionArg =
                    languageLevel
                    |> FSharpLanguageLevel.toLanguageVersion
                    |> FSharpLanguageVersion.toCompilerArg

                otherOptions.Add(langVersionArg)

            let doc = cfg.DocumentationFile
            if not (doc.IsNullOrWhitespace()) then otherOptions.Add("--doc:" + doc)

            let props = cfg.PropertiesCollection

            let getOption f (p: string, compilerArg) =
                let compilerArg = defaultArg compilerArg (p.ToLower())
                match props.TryGetValue(p) with
                | true, v when not (v.IsNullOrWhitespace()) -> Some ("--" + compilerArg + ":" + f v)
                | _ -> None

            [ FSharpProperties.TargetProfile, None; FSharpProperties.LangVersion, None ]
            |> List.choose (getOption id)
            |> otherOptions.AddRange

            [ FSharpProperties.NoWarn, None
              MSBuildProjectUtil.WarningsAsErrorsProperty, Some("warnaserror")
              MSBuildProjectUtil.WarningsNotAsErrorsProperty, Some("warnaserror-") ]
            |> List.choose (getOption (fun v -> (FcsProjectBuilder.splitAndTrim FcsProjectBuilder.itemsDelimiters v).Join(",")))
            |> otherOptions.AddRange

            match props.TryGetValue(FSharpProperties.OtherFlags) with
            | true, otherFlags when not (otherFlags.IsNullOrWhitespace()) -> FcsProjectBuilder.splitAndTrim [| ' ' |] otherFlags
            | _ -> EmptyArray.Instance
            |> otherOptions.AddRange
        | _ -> ()

        let filePaths, implsWithSig, resources = x.GetProjectFilesAndResources(project, targetFrameworkId)

        otherOptions.AddRange(resources |> Seq.map (fun (r: VirtualFileSystemPath) -> "--resource:" + r.FullPath))
        let fileIndices = Dictionary<VirtualFileSystemPath, int>()
        Array.iteri (fun i p -> fileIndices[p] <- i) filePaths

        let projectOptions =
            { ProjectFileName = $"{project.ProjectFileLocation}.{targetFrameworkId}.fsproj"
              ProjectId = None
              SourceFiles = Array.map (fun (p: VirtualFileSystemPath ) -> p.FullPath) filePaths
              OtherOptions = otherOptions.ToArray()
              ReferencedProjects = Array.empty
              IsIncompleteTypeCheckEnvironment = false
              UseScriptResolutionRules = false
              LoadTime = DateTime.Now
              OriginalLoadReferences = List.empty
              UnresolvedReferences = None
              Stamp = None }

        let parsingOptions, errors =
            checkerService.Checker.GetParsingOptionsFromCommandLineArgs(List.ofArray projectOptions.OtherOptions)

        let defines = ImplicitDefines.sourceDefines @ parsingOptions.ConditionalDefines

        let parsingOptions = { parsingOptions with
                                 SourceFiles = projectOptions.SourceFiles
                                 ConditionalDefines = defines }

        if not errors.IsEmpty then
            logger.Warn("Getting parsing options: {0}", concatErrors errors)

        { OutputPath = outPath
          ProjectOptions = projectOptions
          ParsingOptions = parsingOptions
          FileIndices = fileIndices
          ImplementationFilesWithSignatures = implsWithSig
          ReferencedModules = HashSet() }

    member this.AddReferences(fcsProject, referencedPsiModules: IPsiModule seq) =
        fcsProject.ReferencedModules.AddRange(referencedPsiModules)

        let paths =
            referencedPsiModules
            |> Array.ofSeq
            |> Array.map (fun psiModule ->
                let path = modulePathProvider.GetModulePath(psiModule)
                if psiModule :? IProjectPsiModule then
                    fileSystemTracker.AdviseFileChanges(lifetime, path) |> ignore
                path)
            |> Array.map (fun r -> "-r:" + r.FullPath)

        let projectOptions =
            { fcsProject.ProjectOptions with
                OtherOptions = Array.append fcsProject.ProjectOptions.OtherOptions paths
                Stamp = Some(getNextStamp ()) }

        { fcsProject with ProjectOptions = projectOptions }
