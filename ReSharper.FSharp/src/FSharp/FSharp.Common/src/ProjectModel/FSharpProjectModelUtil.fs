[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.ProjectModel.FSharpProjectModelUtil

open JetBrains.Application.Parts
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds

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

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type ModulePathProvider(moduleReferencesResolveStore: IModuleReferencesResolveStore) =
    abstract GetModulePath: reference: IProjectToModuleReference -> VirtualFileSystemPath option
    default this.GetModulePath(reference: IProjectToModuleReference) =
        let referenceTargetFrameworkId = reference.TargetFrameworkId
        match reference.ResolveResult(moduleReferencesResolveStore) with
        | null ->
            // todo: check logic in PsiModuleUtil.TryGetPsiModuleReferences
            None

        | :? IAssembly as assembly ->
            Some assembly.Location.ContainerPhysicalPath

        | :? IProject as project ->
            match referenceTargetFrameworkId.SelectTargetFrameworkIdToReference(project.TargetFrameworkIds) with
            | null -> None
            | targetFrameworkId -> Some(project.GetOutputFilePath(targetFrameworkId))

        | _ -> None
