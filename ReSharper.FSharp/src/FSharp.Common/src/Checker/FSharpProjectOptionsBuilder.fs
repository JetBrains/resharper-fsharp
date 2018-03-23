namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open System.Collections.Generic
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Components
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
        [| "GenerateFSharpInternalsVisibleToFile"
           "GenerateAssemblyFileVersionTask" |]

    interface IMsBuildProjectLoadModificator with
        member x.IsApplicable(mark) =
            match mark with
            | FSharProjectMark -> true
            | _ -> false

        member x.Modify(context) =
            context.Targets.AddRange(targets)

[<SolutionComponent>]
type FSharpProjectOptionsBuilder(solution: ISolution, filesFromTargetsProvider: FSharpProjectFilesFromTargetsProvider) =
    let msBuildHost = solution.ProjectsHostContainer().GetComponent<MsBuildProjectHost>()

    let defaultDelimiters = [| ';'; ','; ' ' |]

    let splitAndTrim (delimiters: char[]) = function
        | null -> Seq.empty
        | (s: string) -> seq {
            for s in s.Split(delimiters) do
                if not (s.IsNullOrWhitespace()) then yield s.Trim() }

    member x.BuildSingleProjectOptions (project: IProject) =
        let properties = project.ProjectProperties
        let buildSettings = properties.BuildSettings :?> _

        let options = ResizeArray()
        options.AddRange(seq {
            yield "--out:" + project.GetOutputFilePath(project.GetCurrentTargetFrameworkId()).FullPath
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

        let paths = x.GetReferencedPathsOptions(project)
        options.AddRange(paths)

        let definedConstants = x.GetDefinedConstants(properties) |> List.ofSeq
        options.AddRange(definedConstants |> Seq.map (fun c -> "--define:" + c))

        match properties.ActiveConfigurations.Configurations.SingleItem() with
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

        let filePaths, pairFiles, resources = x.GetProjectFilesAndResources(project)
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

        { Options = Some projectOptions
          ConfigurationDefines = definedConstants
          FileIndices = fileIndices
          FilesWithPairs = pairFiles
          ParsingOptions = None
        }

    member private x.GetProjectFilesAndResources(project: IProject) =
        let projectMark = project.GetProjectMark().NotNull()
        let projectDir = projectMark.Location.Directory

        let files = Dictionary()
        itemTypes |> Array.iter (fun t -> files.[t] <- ResizeArray())

        let sigFiles = HashSet<string>()
        let pairFiles = HashSet<FileSystemPath>()

        for (RdItem (itemType, path)) in msBuildHost.Session.GetProjectItems(projectMark) do
            if Array.contains itemType itemTypes then
                let path = FileSystemPath.TryParse(path)
                if not path.IsEmpty then
                    let path = ensureAbsolute path projectDir
                    files.[itemType].Add(path)

                    match path with
                    | SigFile -> sigFiles.Add(path.NameWithoutExtension) |> ignore
                    | ImplFile when sigFiles.Contains(path.NameWithoutExtension) -> pairFiles.add(path)
                    | _ -> ()

        let filesFromTargets = filesFromTargetsProvider.GetFilesForProject(projectMark)
        let compileFiles =
            // the order between files lists and filesFromTargets may be wrong
            // and will be fixed when R# keeps a common evaluation order
            // https://youtrack.jetbrains.com/issue/RIDER-9682
            [| yield! files.[CompileBefore]
               yield! filesFromTargets.CompileBefore

               yield! files.[Compile]
               yield! filesFromTargets.Compile

               yield! files.[CompileAfter]
               yield! filesFromTargets.CompileAfter |]

        files.[Resource].AddRange(filesFromTargets.Resource.ToIList())
        compileFiles, pairFiles, files.[Resource]

    member private x.GetReferencedPathsOptions(project: IProject) =
        let framework = project.GetCurrentTargetFrameworkId()
        seq { for p in project.GetReferencedProjects(framework) ->
                  "-r:" + p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()).FullPath
              for a in project.GetAssemblyReferences(framework) do
                  let assemblyFile = a.ResolveResultAssemblyFile()
                  if isNotNull assemblyFile then
                    yield "-r:" + assemblyFile.Location.FullPath }

    member private x.GetOutputType([<CanBeNull>] buildSettings: IManagedProjectBuildSettings) =
        if isNull buildSettings then "library"
        else
            match buildSettings.OutputType with
            | ProjectOutputType.CONSOLE_EXE -> "exe"
            | ProjectOutputType.WIN_EXE -> "winexe"
            | ProjectOutputType.MODULE -> "module"
            | _ -> "library"

    member private x.GetDefinedConstants(properties: IProjectProperties) =
        match properties.ActiveConfigurations.Configurations.SingleItem() with
        | :? IManagedProjectConfiguration as cfg -> splitAndTrim defaultDelimiters cfg.DefineConstants
        | _ -> Seq.empty
