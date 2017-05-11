namespace JetBrains.ReSharper.Plugins.FSharp.Common.ProjectOptions

open System
open System.Collections.Generic
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.Platform.MsBuildModel
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

[<ShellComponent>]
type FSharpProjectPropertiesRequest() =
    let properties = [ "OtherFlags"; "WarnOn" ]
    interface IProjectPropertiesRequest with
        member x.RequestedProperties = properties :> seq<_>

[<SolutionComponent>]
type FSharpProjectOptionsBuilder(solution : ISolution) =
    let msBuildHost = solution.ProjectsHostContainer().GetComponent<MsBuildProjectHost>()
    let defaultDelimiters = [| ';'; ','; ' ' |]

    member x.BuildSingleProjectOptions (project : IProject) =
        let properties = project.ProjectProperties
        let buildSettings = properties.BuildSettings :?> IManagedProjectBuildSettings

        let options = List()
        options.AddRange(seq {
            yield "--out:" + project.GetOutputFilePath(project.GetCurrentTargetFrameworkId()).FullPath
            yield "--simpleresolution"
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

        options.AddRange(x.GetReferencedPathsOptions(project))

        let definedConstants = x.GetDefinedConstants(properties)
        options.AddRange(List.map (fun c -> "--define:" + c) definedConstants)

        match properties.ActiveConfigurations.Configurations.SingleItem() with
        | :? IManagedProjectConfiguration as cfg ->
            options.Add(sprintf "--warn:%d" cfg.WarningLevel)

            let doc = cfg.DocumentationFile
            if not (doc.IsNullOrWhitespace()) then options.Add("--doc:" + doc)

            let nowarn = x.SplitAndTrim(cfg.NoWarn, defaultDelimiters).Join(",")
            if not (nowarn.IsNullOrWhitespace()) then options.Add("--nowarn:" + nowarn)

            let properties = cfg.PropertiesCollection
            options.AddRange(x.SplitAndTrim(properties.TryGetValue("OtherFlags"), ' '))
        | _ -> ()

        let options' =
            { ProjectFileName = project.ProjectFileLocation.FullPath;
              ProjectFileNames = x.GetProjectFileNames(project)
              OtherOptions = options.ToArray()
              ReferencedProjects = [||]
              IsIncompleteTypeCheckEnvironment = false
              UseScriptResolutionRules = false
              LoadTime = DateTime.Now
              OriginalLoadReferences = []
              UnresolvedReferences = None
              ExtraProjectInfo = None }
        
        options', definedConstants
        
    member private x.GetProjectFileNames(project : IProject) =
        let projectMark = project.GetProjectMark().NotNull()
        let projectDir = projectMark.Location.Directory
        let files = List()
        ignore (msBuildHost.mySessionHolder.Execute(fun session ->
            session.EditProject(projectMark, fun editor ->
                for item in editor.Items do
                    if BuildAction.GetOrCreate(item.ItemType()).IsCompile() then
                        let path = FileSystemPath.TryParse(item.EvaluatedInclude)
                        if not path.IsEmpty then
                            files.Add(x.EnsureAbsolute(path, projectDir).FullPath))))
        files.ToArray()

    member private x.GetReferencedPathsOptions(project : IProject) =
        let framework = project.GetCurrentTargetFrameworkId()
        seq { for p in project.GetReferencedProjects(framework) ->
                  "-r:" + p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()).FullPath
              for a in project.GetAssemblyReferences(framework) ->
                  "-r:" + a.ResolveResultAssemblyFile().Location.FullPath }

    member private x.EnsureAbsolute(path : FileSystemPath, projectDirectory : FileSystemPath) : FileSystemPath =
        let relativePath = path.AsRelative()
        if isNull relativePath then path
        else projectDirectory.Combine(relativePath)

    member private x.GetOutputType([<CanBeNull>] buildSettings : IManagedProjectBuildSettings) =
        if isNull buildSettings then "library"
        else
            match buildSettings.OutputType with
            | ProjectOutputType.CONSOLE_EXE -> "exe"
            | ProjectOutputType.WIN_EXE -> "winexe"
            | ProjectOutputType.MODULE -> "module"
            | _ -> "library"

    member private x.GetDefinedConstants(properties : IProjectProperties) =
        match properties.ActiveConfigurations.Configurations.SingleItem() with
        | :? IManagedProjectConfiguration as cfg -> x.SplitAndTrim(cfg.DefineConstants, defaultDelimiters)
        | _ -> List.empty

    member private x.SplitAndTrim([<CanBeNull>] strings, [<ParamArray>] delimiters : char[]) : string list =
        if isNull strings then List.empty
        else [ for s in strings.Split(delimiters) do if not (s.IsNullOrWhitespace()) then yield s.Trim() ]