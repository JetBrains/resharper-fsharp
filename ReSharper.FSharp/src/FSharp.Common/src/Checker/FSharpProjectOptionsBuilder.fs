namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open System.Collections.Generic
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.Platform.MsBuildHost.Models
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
    [<Literal>]
    let TargetProfile = "TargetProfile"

[<ShellComponent>]
type FSharpProjectPropertiesRequest() =
    let properties = [ FSharpProperties.TargetProfile ]
    interface IProjectPropertiesRequest with
        member x.RequestedProperties = properties :> _

[<SolutionComponent>]
type FSharpProjectOptionsBuilder(solution: ISolution,
                                 filesFromTargetsProvider: FSharpProjectFilesFromTargetsProvider) =
    let msBuildHost = solution.ProjectsHostContainer().GetComponent<MsBuildProjectHost>()
    let defaultDelimiters = [| ';'; ','; ' ' |]
    let compileTypes = Set.ofSeq (seq { yield "Compile"; yield "CompileBefore"; yield "CompileAfter"})

    member x.BuildSingleProjectOptions (project: IProject) =
        let properties = project.ProjectProperties
        let buildSettings = properties.BuildSettings :?> _

        let options = List()
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

        let definedConstants = x.GetDefinedConstants(properties)
        options.AddRange(List.map (fun c -> "--define:" + c) definedConstants)

        match properties.ActiveConfigurations.Configurations.SingleItem() with
        | :? IManagedProjectConfiguration as cfg ->
            options.Add(sprintf "--warn:%d" cfg.WarningLevel)

            let doc = cfg.DocumentationFile
            if not (doc.IsNullOrWhitespace()) then options.Add("--doc:" + doc)

            let nowarn = x.SplitAndTrim(cfg.NoWarn, defaultDelimiters).Join(",")
            if not (nowarn.IsNullOrWhitespace()) then options.Add("--nowarn:" + nowarn)
            
            let props = cfg.PropertiesCollection
            let targetProfile = ref ""
            match props.TryGetValue(FSharpProperties.TargetProfile, targetProfile), !targetProfile with
            | true, targetProfile when not (targetProfile.IsNullOrWhitespace()) ->
                options.Add("--targetprofile:" + targetProfile.Trim())
            | _ -> ()
        | _ -> ()

        let filePaths, pairFiles = x.GetProjectFiles(project)
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

    member private x.GetProjectFiles(project: IProject) =
        let projectMark = project.GetProjectMark().NotNull()
        let projectDir = projectMark.Location.Directory
        let files = List()
        let sigFiles = HashSet<string>()
        let pairFiles = HashSet<FileSystemPath>()
        ignore (msBuildHost.mySessionHolder.Execute(fun session ->
            session.TryEditProject(projectMark, fun editor ->
                for item in editor.Items do
                    if compileTypes.Contains(item.ItemType()) then
                        let path = FileSystemPath.TryParse(item.EvaluatedInclude)
                        if not path.IsEmpty then
                            let path = ensureAbsolute path projectDir
                            files.Add(path)
                            if path.IsSigFile() then sigFiles.add path.NameWithoutExtension
                            else if path.IsImplFile() && sigFiles.Contains(path.NameWithoutExtension)
                                 then pairFiles.add(path))))
        let filesFromTargets = filesFromTargetsProvider.GetFilesForProject(projectMark)
        let files =
            seq { yield! filesFromTargets.CompileBefore
                  yield! filesFromTargets.Compile
                  yield! files
                  yield! filesFromTargets.CompileAfter }
        files |> Array.ofSeq, pairFiles

    member private x.GetReferencedPathsOptions(project: IProject) =
        let framework = project.GetCurrentTargetFrameworkId()
        seq { for p in project.GetReferencedProjects(framework) ->
                  "-r:" + p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()).FullPath
              for a in project.GetAssemblyReferences(framework) ->
                  "-r:" + a.ResolveResultAssemblyFile().Location.FullPath }

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
        | :? IManagedProjectConfiguration as cfg -> x.SplitAndTrim(cfg.DefineConstants, defaultDelimiters)
        | _ -> List.empty

    member private x.SplitAndTrim([<CanBeNull>] strings, [<ParamArray>] delimiters: char[]): string list =
        if isNull strings then List.empty
        else [ for s in strings.Split(delimiters) do if not (s.IsNullOrWhitespace()) then yield s.Trim() ]
