namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open System.Collections.Generic
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.Metadata.Reader.API
open JetBrains.Platform.MsBuildHost.Models
open JetBrains.Platform.MsBuildHost.ProjectModel
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

module FSharpProperties =
    let [<Literal>] TargetProfile = "TargetProfile"
    let [<Literal>] BaseAddress = "BaseAddress"
    let [<Literal>] OtherFlags = "OtherFlags"
    let [<Literal>] NoWarn = "NoWarn"
    let [<Literal>] WarnAsError = "WarnAsError"


[<ShellComponent>]
type FSharpProjectPropertiesRequest() =
    let properties =
        [ FSharpProperties.TargetProfile
          FSharpProperties.BaseAddress
          FSharpProperties.OtherFlags
          FSharpProperties.NoWarn
          FSharpProperties.WarnAsError ]

    interface IProjectPropertiesRequest with
        member x.RequestedProperties = properties :> _


[<ShellComponent>]
type VisualFSharpTargetsProjectLoadModificator() =
    let targets =
        [| "GenerateCode"
           "GenerateFSharpInternalsVisibleToFile"
           "GenerateAssemblyFileVersionTask" |]

    interface IMsBuildProjectLoadModificator with
        member x.IsApplicable(mark) =
            match mark with
            | FSharProjectMark -> true
            | _ -> false

        member x.Modify(context) =
            context.Targets.AddRange(targets)


type FSharpProject =
    { Options: FSharpProjectOptions
      ParsingOptions: FSharpParsingOptions
      ConfigurationDefines: string list
      FileIndices: IDictionary<FileSystemPath, int>
      FilesWithPairs: ISet<FileSystemPath> }

    member x.ContainsFile (file: IPsiSourceFile) =
        x.FileIndices.ContainsKey(file.GetLocation())


[<SolutionComponent>]
type FSharpProjectOptionsBuilder
        (solution: ISolution, checkerService: FSharpCheckerService, psiModules: IPsiModules, logger: ILogger,
         psiModulesResolveContextManager: PsiModuleResolveContextManager, itemsContainer: IFSharpItemsContainer) =
    let msBuildHost = solution.ProjectsHostContainer().GetComponent<MsBuildProjectHost>()

    let defaultDelimiters = [| ';'; ','; ' ' |]

    let splitAndTrim (delimiters: char[]) = function
        | null -> Seq.empty
        | (s: string) -> seq {
            for s in s.Split(delimiters) do
                if not (s.IsNullOrWhitespace()) then yield s.Trim() }

    let getReferences project psiModule targetFrameworkId =
        let resolveContext =
            psiModulesResolveContextManager.GetOrCreateModuleResolveContext(project, psiModule, targetFrameworkId)

        psiModules.GetModuleReferences(psiModule, resolveContext)
        |> Seq.choose (fun reference ->
            let targetFrameworkId = reference.Module.TargetFrameworkId
            match reference.Module.ContainingProjectModule with
            | :? IProject as project -> Some (project.GetOutputFilePath(targetFrameworkId))
            | :? IAssembly as assembly -> Some (assembly.GetLocation())
            | _ -> None)
        |> Seq.map (fun path -> "-r:" + path.FullPath)

    member x.BuildSingleProjectOptions (project: IProject, psiModule: IPsiModule) =
        let targetFrameworkId = psiModule.TargetFrameworkId
        let properties = project.ProjectProperties
        let buildSettings = properties.BuildSettings :?> _ // todo: can differ by framework id?

        let options = ResizeArray()
        options.AddRange(seq {
            yield "--out:" + project.GetOutputFilePath(targetFrameworkId).FullPath
            yield "--noframework"
            yield "--debug:full"
            yield "--debug+"
            yield "--optimize-"
            yield "--tailcalls-"
            yield "--fullpaths"
            yield "--flaterrors"
            yield "--highentropyva+"
            yield "--target:" + x.GetOutputType(buildSettings)
          })

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
            | _ -> Seq.empty
            |> options.AddRange
        | _ -> ()

        let filePaths, pairFiles, resources = x.GetProjectFilesAndResources(project, targetFrameworkId)
        options.AddRange(resources |> Seq.map (fun (r: FileSystemPath) -> "--resource:" + r.FullPath))
        let fileIndices = Dictionary<FileSystemPath, int>()
        Array.iteri (fun i p -> fileIndices.[p] <- i) filePaths

        let projectOptions =
            { ProjectFileName = project.ProjectFileLocation.FullPath
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
          ConfigurationDefines = definedConstants
          FileIndices = fileIndices
          FilesWithPairs = pairFiles
          ParsingOptions = parsingOptions }

    member private x.GetProjectFilesAndResources(project: IProject, targetFrameworkId: TargetFrameworkId) =
        let projectMark = project.GetProjectMark().NotNull()

        let sourceFiles = List()
        let resources = List()

        let sigFiles = HashSet<string>()
        let pairFiles = HashSet<FileSystemPath>()

        let projectItems = itemsContainer.GetProjectItemsPaths(projectMark, targetFrameworkId)
        for path, buildAction in projectItems do
            match buildAction with
            | SourceFile ->
                sourceFiles.Add(path) |> ignore
                match path with
                | SigFile -> sigFiles.Add(path.NameWithoutExtension) |> ignore
                | ImplFile when sigFiles.Contains(path.NameWithoutExtension) -> pairFiles.add(path)
                | _ -> ()

            | Resource -> resources.Add(path) |> ignore
            | _ -> ()

        sourceFiles.ToArray(), pairFiles, resources

    member private x.GetOutputType([<CanBeNull>] buildSettings: IManagedProjectBuildSettings) =
        if isNull buildSettings then "library"
        else
            match buildSettings.OutputType with
            | ProjectOutputType.CONSOLE_EXE -> "exe"
            | ProjectOutputType.WIN_EXE -> "winexe"
            | ProjectOutputType.MODULE -> "module"
            | _ -> "library"

    member private x.GetDefinedConstants(properties: IProjectProperties, targetFrameworkId: TargetFrameworkId) =
        match properties.ActiveConfigurations.GetOrCreateConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg -> splitAndTrim defaultDelimiters cfg.DefineConstants
        | _ -> Seq.empty
