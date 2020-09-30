[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.ProjectModel.FSharpProjectModelUtil

open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi.Modules

let getResolveContext (resolveContextManager: PsiModuleResolveContextManager) project psiModule =
    resolveContextManager.GetOrCreateModuleResolveContext(project, psiModule, psiModule.TargetFrameworkId)

let getModuleResolveContext (resolveContextManager: PsiModuleResolveContextManager) (psiModule: IPsiModule) =
    let project = psiModule.ContainingProjectModule :?> IProject
    getResolveContext resolveContextManager project psiModule


let getReferencedModules (psiModule: IPsiModule) =
    let project = psiModule.ContainingProjectModule :?> _
    let solution = psiModule.GetSolution()

    let psiModules = solution.PsiModules()
    let resolveContextManager = solution.GetComponent<PsiModuleResolveContextManager>()

    let resolveContext = getResolveContext resolveContextManager project psiModule
    psiModules.GetModuleReferences(psiModule, resolveContext)
    |> Seq.filter (fun reference ->
        match reference.Module with
        | :? IProjectPsiModule as projectPsiModule -> projectPsiModule != project
        | _ -> true)
    |> Seq.map (fun reference -> reference.Module)

let getModulePath (psiModule: IPsiModule) =
    match psiModule with
    | :? IAssemblyPsiModule as assemblyPsiModule ->
        assemblyPsiModule.Assembly.Location

    | :? IProjectPsiModule as projectPsiModule ->
        projectPsiModule.Project.GetOutputFilePath(projectPsiModule.TargetFrameworkId)

    | _ -> FileSystemPath.Empty

let getModuleFullPath (psiModule: IPsiModule) =
    let path = getModulePath psiModule
    path.FullPath


let getOutputPath (psiModule: IPsiModule) =
    match psiModule.ContainingProjectModule with
    | :? IProject as project -> project.GetOutputFilePath(psiModule.TargetFrameworkId)
    | _ -> FileSystemPath.Empty
