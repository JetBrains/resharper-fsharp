namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System.Linq
open System.Collections.Generic
open JetBrains
open JetBrains.Application
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.Platform.MsBuildHost.Models
open JetBrains.ProjectModel
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Common.Util

type FileSystemPath = Util.FileSystemPath

type ProjectFiles =
    {
        Compile:       FileSystemPath list
        CompileBefore: FileSystemPath list
        CompileAfter:  FileSystemPath list
    }
    static member Empty = {Compile = []; CompileBefore = []; CompileAfter = []}

// todo: this component should be removed when R# is refactored

/// this is a temp class until items from CompileBefore/After are preserved somehow in R#
[<SolutionInstanceComponent>]
type FSharpProjectFilesFromTargetsProvider(lifetime: Lifetime) =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    let projects = Dictionary<IProjectMark, ProjectFiles>() // todo: multiple frameworks

    member x.GetFilesForProject(projectMark) =
        match projects.TryGetValue(projectMark) with
        | true, files -> files
        | _ -> ProjectFiles.Empty

    member x.RemoveProject(projectMark) =
        lock projects (fun _ ->
        projects.Remove(projectMark) |> ignore)

    interface IMsBuildProjectListener with
        member x.OnProjectLoaded(projectMark, msBuildProject) =
            if isNotNull msBuildProject && msBuildProject.RdProjects.Any() then
                lock projects (fun _ ->
                let compile = List()
                let compileBefore = List()
                let compileAfter = List()

                let rdProject = msBuildProject.RdProjects.First().Value // take project with first framework
                let projectDir = FileSystemPath.TryParse(msBuildProject.RdProjectDescription.Directory)
                for item in rdProject.Items do
                    match item.Id with
                    // todo: report/investigate sometimes FCS shows error about RdEvaluatedProjectItemId type
                    | :? RdEvaluatedProjectItemId as itemId ->
                        let filesList = 
                            match itemId.ItemType with
                            | "Compile"       -> compile
                            | "CompileBefore" -> compileBefore
                            | "CompileAfter"  -> compileAfter
                            | _ -> null
                        if isNotNull filesList then
                            let path = FileSystemPath.TryParse(item.EvaluatedInclude)
                            if not path.IsEmpty then
                                filesList.Add(ensureAbsolute path projectDir)
                    | _ -> ()
                projects.[projectMark] <- { Compile       = List.ofSeq compile
                                            CompileBefore = List.ofSeq compileBefore
                                            CompileAfter  = List.ofSeq compileAfter })
