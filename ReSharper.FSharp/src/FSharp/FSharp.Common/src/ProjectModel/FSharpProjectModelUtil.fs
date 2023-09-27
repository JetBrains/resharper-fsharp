[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.ProjectModel.FSharpProjectModelUtil

open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util

let getResolveContext (resolveContextManager: PsiModuleResolveContextManager) project psiModule =
    resolveContextManager.GetOrCreateModuleResolveContext(project, psiModule, psiModule.TargetFrameworkId)

let getModuleResolveContext (resolveContextManager: PsiModuleResolveContextManager) (psiModule: IPsiModule) =
    let project = psiModule.ContainingProjectModule :?> IProject
    getResolveContext resolveContextManager project psiModule

let getModuleProject (psiModule: IPsiModule) =
    psiModule.ContainingProjectModule.As<IProject>()

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

module ModulePathProvider =
    let outputPathKey = Key<VirtualFileSystemPath>("AssemblyReaderTest.outputPath")

[<SolutionComponent>]
type ModulePathProvider() =
    abstract GetModulePath: psiModule: IPsiModule -> VirtualFileSystemPath
    default this.GetModulePath(psiModule) =
        match psiModule with
        | :? IAssemblyPsiModule as assemblyPsiModule ->
            assemblyPsiModule.Assembly.Location.AssemblyPhysicalPath

        | :? IProjectPsiModule as projectPsiModule ->
            projectPsiModule.Project.GetOutputFilePath(projectPsiModule.TargetFrameworkId)

        | _ -> VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext)
