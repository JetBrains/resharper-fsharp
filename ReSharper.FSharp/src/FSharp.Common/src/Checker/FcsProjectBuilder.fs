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
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds

type FcsProject =
    { OutputPath: FileSystemPath
      FileIndices: Dictionary<FileSystemPath, int>
      ProjectOptions: FSharpProjectOptions
      ParsingOptions: FSharpParsingOptions
      ImplementationFilesWithSignatures: ISet<FileSystemPath> }

    member x.IsKnownFile(sourceFile: IPsiSourceFile) =
        x.FileIndices.ContainsKey(sourceFile.GetLocation())


type ReferencedModule =
    { ReferencedPath: FileSystemPath
      ReferencingModules: HashSet<IPsiModule> }

    static member Create(psiModule: IPsiModule) =
        { ReferencedPath = getOutputPath psiModule
          ReferencingModules = HashSet() }


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

[<AutoOpen>]
module ProjectOptions =
    let sandboxParsingOptions =
        { FSharpParsingOptions.Default with SourceFiles = [| "Sandbox.fs" |] }

    [<RequireQualifiedAccess>]
    module ImplicitDefines =
        let sourceDefines = [ "EDITING"; "COMPILED" ]
        let scriptDefines = [ "EDITING"; "INTERACTIVE" ]

        let getImplicitDefines isScript =
            if isScript then scriptDefines else sourceDefines


[<SolutionComponent>]
type FcsProjectBuilder(checkerService: FSharpCheckerService, itemsContainer: IFSharpItemsContainer, logger: ILogger) =

    let itemsDelimiters = [| ';'; ','; ' ' |]

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

    let splitAndTrim (delimiters: char[]) (s: string) =
        if isNull s then EmptyArray.Instance else
        s.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)

    let getReferences psiModule =
        getReferencedModules psiModule
        |> Seq.map getModuleFullPath
        |> Seq.map (fun r -> "-r:" + r)

    let getOutputType (outputType: ProjectOutputType) =
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
        logger.Verbose("Creating FcsProject: {0}", psiModule)

        let targetFrameworkId = psiModule.TargetFrameworkId
        let projectProperties = project.ProjectProperties

        let otherOptions = List()

        let outPath = project.GetOutputFilePath(targetFrameworkId)
        if not outPath.IsEmpty then
            otherOptions.Add("--out:" + outPath.FullPath)

        otherOptions.AddRange(defaultOptions)
        otherOptions.AddRange(unusedValuesWarns)
        otherOptions.AddRange(getReferences psiModule)

        match projectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg ->
            let definedConstants = splitAndTrim itemsDelimiters cfg.DefineConstants
            otherOptions.AddRange(definedConstants |> Seq.map (fun c -> "--define:" + c))

            otherOptions.Add("--target:" + getOutputType cfg.OutputType)

            otherOptions.Add(sprintf "--warn:%d" cfg.WarningLevel)

            if cfg.TreatWarningsAsErrors then
                otherOptions.Add("--warnaserror")

            let doc = cfg.DocumentationFile
            if not (doc.IsNullOrWhitespace()) then otherOptions.Add("--doc:" + doc)

            let props = cfg.PropertiesCollection

            let getOption f p =
                match props.TryGetValue(p) with
                | true, v when not (v.IsNullOrWhitespace()) -> Some ("--" + p.ToLower() + ":" + f v)
                | _ -> None

            [ FSharpProperties.TargetProfile; FSharpProperties.BaseAddress; FSharpProperties.LangVersion ]
            |> List.choose (getOption id)
            |> otherOptions.AddRange

            [ FSharpProperties.NoWarn; FSharpProperties.WarnAsError ]
            |> List.choose (getOption (fun v -> (splitAndTrim itemsDelimiters v).Join(",")))
            |> otherOptions.AddRange

            match props.TryGetValue(FSharpProperties.OtherFlags) with
            | true, otherFlags when not (otherFlags.IsNullOrWhitespace()) -> splitAndTrim [| ' ' |] otherFlags
            | _ -> EmptyArray.Instance
            |> otherOptions.AddRange
        | _ -> ()

        let filePaths, implsWithSig, resources = x.GetProjectFilesAndResources(project, targetFrameworkId)

        otherOptions.AddRange(resources |> Seq.map (fun (r: FileSystemPath) -> "--resource:" + r.FullPath))
        let fileIndices = Dictionary<FileSystemPath, int>()
        Array.iteri (fun i p -> fileIndices.[p] <- i) filePaths

        let projectOptions =
            { ProjectFileName = sprintf "%O.%O.fsproj" project.ProjectFileLocation targetFrameworkId
              ProjectId = None
              SourceFiles = Array.map (fun (p: FileSystemPath ) -> p.FullPath) filePaths
              OtherOptions = otherOptions.ToArray()
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

        let defines = ImplicitDefines.sourceDefines @ parsingOptions.ConditionalCompilationDefines

        let parsingOptions = { parsingOptions with
                                    SourceFiles = options.SourceFiles
                                    ConditionalCompilationDefines = defines }

        if not errors.IsEmpty then
            logger.Warn("Getting parsing options: {0}", concatErrors errors)

        { OutputPath = outPath
          ProjectOptions = projectOptions
          ParsingOptions = parsingOptions
          FileIndices = fileIndices
          ImplementationFilesWithSignatures = implsWithSig }
