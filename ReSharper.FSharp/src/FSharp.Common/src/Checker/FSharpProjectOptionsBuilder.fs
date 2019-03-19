namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open System.Collections.Generic
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Assemblies.Impl
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds
open Microsoft.FSharp.Compiler.SourceCodeServices

module FSharpProperties =
    let [<Literal>] TargetProfile = "TargetProfile"
    let [<Literal>] BaseAddress = "BaseAddress"
    let [<Literal>] OtherFlags = "OtherFlags"
    let [<Literal>] NoWarn = "NoWarn"
    let [<Literal>] WarnAsError = "WarnAsError"
    let [<Literal>] FscToolPath = "FscToolPath"


[<ShellComponent>]
type FSharpProjectPropertiesRequest() =
    let properties =
        [ FSharpProperties.TargetProfile
          FSharpProperties.BaseAddress
          FSharpProperties.OtherFlags
          FSharpProperties.NoWarn
          FSharpProperties.WarnAsError
          FSharpProperties.FscToolPath ]

    interface IProjectPropertiesRequest with
        member x.RequestedProperties = properties :> _


[<ShellComponent>]
type FSharpTargetsProjectLoadModificator() =
    let targets =
        [| "GenerateCode"
           "GenerateFSharpInternalsVisibleToFile"
           "GenerateAssemblyFileVersionTask"
           "ImplicitlyExpandNETStandardFacades" |]

    interface IMsBuildProjectLoadModificator with
        member x.IsApplicable(mark) =
            match mark with
            | FSharpProjectMark -> true
            | _ -> false

        member x.Modify(context) =
            context.Targets.AddRange(targets)


type FSharpProject =
    { Options: FSharpProjectOptions
      ParsingOptions: FSharpParsingOptions
      FileIndices: IDictionary<FileSystemPath, int>
      ImplFilesWithSigs: ISet<FileSystemPath> }

    member x.ContainsFile(file: IPsiSourceFile) =
        x.FileIndices.ContainsKey(file.GetLocation())


[<SolutionComponent>]
type FSharpProjectOptionsBuilder
        (checkerService: FSharpCheckerService, psiModules: IPsiModules, logger: ILogger,
         resolveContextManager: ResolveContextManager, itemsContainer: IFSharpItemsContainer) =

    let defaultDelimiters = [| ';'; ','; ' ' |]

    let defaultOptions =
        [| "--noframework"
           "--debug:full"
           "--debug+"
           "--optimize-"
           "--tailcalls-"
           "--fullpaths"
           "--flaterrors"
           "--highentropyva+" |]

    let unusedValuesWarns =
        [| "--warnon:1182"
           "--warnaswarn:1182" |]

    let splitAndTrim (delimiters: char[]) = function
        | null -> EmptyArray.Instance
        | (s: string) -> s.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)

    let getReferences project psiModule targetFrameworkId =
        let result = List()
        let resolveContext = resolveContextManager.GetOrCreateProjectResolveContext(project, targetFrameworkId)
        for reference in psiModules.GetModuleReferences(psiModule, resolveContext) do
            match reference.Module.ContainingProjectModule with
            | :? IProject as referencedProject when referencedProject <> project ->
                result.Add("-r:" + referencedProject.GetOutputFilePath(reference.Module.TargetFrameworkId).FullPath)
            | :? IAssembly as assembly -> result.Add("-r:" + assembly.GetLocation().FullPath)
            | _ -> ()

        result

    member x.BuildSingleProjectOptions (project: IProject, psiModule: IPsiModule) =
        let targetFrameworkId = psiModule.TargetFrameworkId
        let properties = project.ProjectProperties
        let buildSettings = properties.BuildSettings :?> _ // todo: can differ by framework id?

        let options = List()
        options.Add("--out:" + project.GetOutputFilePath(targetFrameworkId).FullPath)
        options.Add("--target:" + x.GetOutputType(buildSettings))
        options.AddRange(defaultOptions)
        options.AddRange(unusedValuesWarns)
        options.AddRange(getReferences project psiModule targetFrameworkId)

        let definedConstants = x.GetDefinedConstants(properties, targetFrameworkId) |> List.ofSeq
        options.AddRange(definedConstants |> Seq.map (fun c -> "--define:" + c))

        match properties.ActiveConfigurations.GetOrCreateConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg ->
            options.Add(sprintf "--warn:%d" cfg.WarningLevel)

            let doc = cfg.DocumentationFile
            if not (doc.IsNullOrWhitespace()) then options.Add("--doc:" + doc)

            let props = cfg.PropertiesCollection

            let getOption f p =
                match props.TryGetValue(p) with
                | v when not (v.IsNullOrWhitespace()) -> Some ("--" + p.ToLower() + ":" + f v)
                | _ -> None

            [FSharpProperties.TargetProfile; FSharpProperties.BaseAddress]
            |> List.choose (getOption id)
            |> options.AddRange

            [FSharpProperties.NoWarn; FSharpProperties.WarnAsError]
            |> List.choose (getOption (fun v -> (splitAndTrim defaultDelimiters v).Join(",")))
            |> options.AddRange

            match props.TryGetValue(FSharpProperties.OtherFlags) with
            | otherFlags when not (otherFlags.IsNullOrWhitespace()) -> splitAndTrim [| ' ' |] otherFlags
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

        let shoudAddFSharpCore options =
            not (hasFSharpCoreReference options || options.OtherOptions |> Array.contains "--compiling-fslib")

        let options =
            if shoudAddFSharpCore projectOptions then 
                { projectOptions with
                    OtherOptions = FSharpCoreFix.ensureCorrectFSharpCore projectOptions.OtherOptions }
            else projectOptions

        let parsingOptions, errors =
            checkerService.Checker.GetParsingOptionsFromCommandLineArgs(List.ofArray options.OtherOptions)

        let parsingOptions = { parsingOptions with SourceFiles = options.SourceFiles }
        if not errors.IsEmpty then
            logger.Warn("Getting parsing options: {0}", concatErrors errors)

        { Options = projectOptions
          ParsingOptions = parsingOptions
          FileIndices = fileIndices
          ImplFilesWithSigs = implsWithSig }

    member x.GetProjectFilesAndResources(project: IProject, targetFrameworkId) =
        let sourceFiles = List()
        let resources = List()

        let sigFiles = HashSet()
        let implsWithSigs = HashSet()

        let projectMark = project.GetProjectMark().NotNull("projectMark == null")
        let projectItems = itemsContainer.GetProjectItemsPaths(projectMark, targetFrameworkId)

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

    member private x.GetOutputType([<CanBeNull>] buildSettings: IManagedProjectBuildSettings) =
        if isNull buildSettings then "library" else

        match buildSettings.OutputType with
        | ProjectOutputType.CONSOLE_EXE -> "exe"
        | ProjectOutputType.WIN_EXE -> "winexe"
        | ProjectOutputType.MODULE -> "module"
        | _ -> "library"

    member private x.GetDefinedConstants(properties: IProjectProperties, targetFrameworkId: TargetFrameworkId) =
        match properties.ActiveConfigurations.GetOrCreateConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg -> splitAndTrim defaultDelimiters cfg.DefineConstants
        | _ -> EmptyArray.Instance
