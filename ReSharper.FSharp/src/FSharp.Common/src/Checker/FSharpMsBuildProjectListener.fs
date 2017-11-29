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
        Resource:      FileSystemPath list
    }
    static member Empty = { Compile = []; CompileBefore = []; CompileAfter = []; Resource = [] }

[<AutoOpen>]
module FSharpItemTypes =
    let [<Literal>] Compile = "Compile"
    let [<Literal>] CompileBefore = "CompileBefore"
    let [<Literal>] CompileAfter = "CompileAfter"
    let [<Literal>] Resource = "Resource"

    let itemTypes =
        [| Compile
           CompileBefore
           CompileAfter
           Resource |]

/// Getting evaluated items from other tasks, e.g. FSComp.fs earlier in visualfsharp.
/// This class should be removed when R# preserves these items in the correct order.
[<SolutionInstanceComponent>]
type FSharpProjectFilesFromTargetsProvider(lifetime: Lifetime) =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    let projects = Dictionary<IProjectMark, ProjectFiles>() // todo: multiple frameworks

    member x.GetFilesForProject(projectMark) =
        match projects.TryGetValue(projectMark) with
        | true, files -> files
        | _ -> ProjectFiles.Empty

    member x.RemoveProject(projectMark) =
        lock projects (fun _ -> projects.Remove(projectMark) |> ignore)

    interface IMsBuildProjectListener with
        member x.OnProjectLoaded(projectMark, msBuildProject) =
            if isNotNull msBuildProject && msBuildProject.RdProjects.Any() then
                match projectMark with
                | FSharProjectMark ->
                    lock projects (fun _ ->
                    let files = Dictionary()
                    itemTypes |> Array.iter (fun t -> files.[t] <- ResizeArray())
    
                    let rdProject = msBuildProject.RdProjects.First().Value // take project with first framework
                    let projectDir = FileSystemPath.TryParse(msBuildProject.RdProjectDescription.Directory)
                    for item in rdProject.Items do
                        match item.Id with
                        | :? RdEvaluatedProjectItemId as itemId ->
                            if Array.contains itemId.ItemType itemTypes then
                                let path = FileSystemPath.TryParse(item.EvaluatedInclude)
                                if not path.IsEmpty then
                                    let path = ensureAbsolute path projectDir
                                    files.[itemId.ItemType].Add(path)
                        | _ -> ()
                    projects.[projectMark] <-
                        { Compile       = List.ofSeq files.[Compile]
                          CompileBefore = List.ofSeq files.[CompileBefore]
                          CompileAfter  = List.ofSeq files.[CompileAfter]
                          Resource      = List.ofSeq files.[Resource] })
                | _ -> ()
