namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Strategies
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds

module FSharpProperties =
    let [<Literal>] TargetProfile = "TargetProfile"
    let [<Literal>] BaseAddress = "BaseAddress"
    let [<Literal>] OtherFlags = "OtherFlags"
    let [<Literal>] NoWarn = "NoWarn"
    let [<Literal>] WarnAsError = "WarnAsError"
    let [<Literal>] FscToolPath = "FscToolPath"
    let [<Literal>] LangVersion = "LangVersion"


[<ShellComponent>]
type FSharpProjectPropertiesRequest() =
    let properties =
        [| FSharpProperties.TargetProfile
           FSharpProperties.BaseAddress
           FSharpProperties.OtherFlags
           FSharpProperties.NoWarn
           FSharpProperties.WarnAsError
           FSharpProperties.FscToolPath
           FSharpProperties.LangVersion |]

    interface IProjectPropertiesRequest with
        member x.RequestedProperties = properties :> _


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


type IFSharpProjectOptionsBuilder =
    abstract BuildSingleFSharpProject: IProject * IPsiModule -> FSharpProject


[<SolutionComponent>]
type FSharpProjectOptionsBuilder
        (checkerService: FSharpCheckerService, logger: ILogger, itemsContainer: IFSharpItemsContainer) =

    let defaultDelimiters = [| ';'; ','; ' ' |]

    let defaultOptions =
        [| "--noframework"
           "--debug:full"
           "--debug+"
           "--optimize-"
           "--tailcalls-"
           "--fullpaths"
           "--flaterrors"
           "--highentropyva+"
           "--noconditionalerasure" |]

    let unusedValuesWarns =
        [| "--warnon:1182" |]

    let splitAndTrim (delimiters: char[]) = function
        | null -> EmptyArray.Instance
        | (s: string) -> s.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)

    let getReferences psiModule =
        getReferencePaths (fun _ -> true) psiModule
        |> Seq.map (fun r -> "-r:" + r)

    let getOutputType outputType =
        match outputType with
        | ProjectOutputType.CONSOLE_EXE -> "exe"
        | ProjectOutputType.WIN_EXE -> "winexe"
        | ProjectOutputType.MODULE -> "module"
        | _ -> "library"

    abstract GetProjectItemsPaths:
        project: IProject * targetFrameworkId: TargetFrameworkId -> (FileSystemPath * BuildAction)[]

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
                sourceFiles.Add(path) |> ignore
                let fileName = path.NameWithoutExtension
                match path.ExtensionNoDot with
                | SigExtension -> sigFiles.Add(fileName) |> ignore
                | ImplExtension when sigFiles.Contains(fileName) -> implsWithSigs.add(path)
                | _ -> ()

            | Resource -> resources.Add(path) |> ignore
            | _ -> ()

        let resources: IList<_> = if resources.IsEmpty() then EmptyList.InstanceList else resources :> _
        let implsWithSigs: ISet<_> = if implsWithSigs.IsEmpty() then EmptySet.Instance :> _ else implsWithSigs :> _

        sourceFiles.ToArray(), implsWithSigs, resources

    interface IFSharpProjectOptionsBuilder with
        member x.BuildSingleFSharpProject(project: IProject, psiModule: IPsiModule) =
            let targetFrameworkId = psiModule.TargetFrameworkId
            let properties = project.ProjectProperties

            let options = List()

            let outPath = project.GetOutputFilePath(targetFrameworkId)
            if not outPath.IsEmpty then
                options.Add("--out:" + outPath.FullPath)

            options.AddRange(defaultOptions)
            options.AddRange(unusedValuesWarns)
            options.AddRange(getReferences psiModule)

            match properties.ActiveConfigurations.GetOrCreateConfiguration(targetFrameworkId) with
            | :? IManagedProjectConfiguration as cfg ->
                let definedConstants = splitAndTrim defaultDelimiters cfg.DefineConstants
                options.AddRange(definedConstants |> Seq.map (fun c -> "--define:" + c))

                options.Add("--target:" + getOutputType cfg.OutputType)

                options.Add(sprintf "--warn:%d" cfg.WarningLevel)

                if cfg.TreatWarningsAsErrors then
                    options.Add("--warnaserror")

                let doc = cfg.DocumentationFile
                if not (doc.IsNullOrWhitespace()) then options.Add("--doc:" + doc)

                let props = cfg.PropertiesCollection

                let getOption f p =
                    match props.TryGetValue(p) with
                    | true, v when not (v.IsNullOrWhitespace()) -> Some ("--" + p.ToLower() + ":" + f v)
                    | _ -> None

                [ FSharpProperties.TargetProfile; FSharpProperties.BaseAddress; FSharpProperties.LangVersion ]
                |> List.choose (getOption id)
                |> options.AddRange

                [ FSharpProperties.NoWarn; FSharpProperties.WarnAsError ]
                |> List.choose (getOption (fun v -> (splitAndTrim defaultDelimiters v).Join(",")))
                |> options.AddRange

                match props.TryGetValue(FSharpProperties.OtherFlags) with
                | true, otherFlags when not (otherFlags.IsNullOrWhitespace()) -> splitAndTrim [| ' ' |] otherFlags
                | _ -> EmptyArray.Instance
                |> options.AddRange
            | _ -> ()

            let filePaths, implsWithSig, resources = x.GetProjectFilesAndResources(project, targetFrameworkId)

            options.AddRange(resources |> Seq.map (fun (r: FileSystemPath) -> "--resource:" + r.FullPath))
            let fileIndices = Dictionary<FileSystemPath, int>()
            Array.iteri (fun i p -> fileIndices.[p] <- i) filePaths

            let projectOptions =
                { ProjectFileName = sprintf "%O.%O.fsproj" project.ProjectFileLocation targetFrameworkId
                  ProjectId = None
                  SourceFiles = Array.map (fun (p: FileSystemPath ) -> p.FullPath) filePaths
                  OtherOptions = options.ToArray()
                  ReferencedProjects = Array.empty
                  IsIncompleteTypeCheckEnvironment = false
                  UseScriptResolutionRules = false
                  LoadTime = DateTime.Now
                  OriginalLoadReferences = List.empty
                  UnresolvedReferences = None
                  ExtraProjectInfo = None
                  Stamp = None }

            let hasFSharpCoreReference options =
                options.OtherOptions
                |> Seq.exists (fun s ->
                    s.StartsWith("-r:", StringComparison.Ordinal) &&
                    s.EndsWith("FSharp.Core.dll", StringComparison.Ordinal))

            let shouldAddFSharpCore options =
                not (hasFSharpCoreReference options || options.OtherOptions |> Array.contains "--compiling-fslib")

            let options =
                if shouldAddFSharpCore projectOptions then 
                    { projectOptions with
                        OtherOptions = FSharpCoreFix.ensureCorrectFSharpCore projectOptions.OtherOptions }
                else projectOptions

            let parsingOptions, errors =
                checkerService.Checker.GetParsingOptionsFromCommandLineArgs(List.ofArray options.OtherOptions)

            let parsingOptions = { parsingOptions with SourceFiles = options.SourceFiles }
            if not errors.IsEmpty then
                logger.Warn("Getting parsing options: {0}", concatErrors errors)

            { ProjectOptions = projectOptions
              ParsingOptions = parsingOptions
              FileIndices = fileIndices
              ImplFilesWithSigs = implsWithSig }
