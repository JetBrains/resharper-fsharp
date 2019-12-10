[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.ProjectModel.FSharpProjectModelUtil

open System.Collections.Generic
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Assemblies.Impl
open JetBrains.ReSharper.Psi.Modules

let getReferencePaths assemblyFilter (psiModule: IPsiModule) =
    let project = psiModule.ContainingProjectModule :?> _
    let solution = psiModule.GetSolution()

    let psiModules = solution.PsiModules()
    let resolveContextManager = solution.GetComponent<ResolveContextManager>()

    let result = List()
    let resolveContext = resolveContextManager.GetOrCreateProjectResolveContext(project, psiModule.TargetFrameworkId)
    for reference in psiModules.GetModuleReferences(psiModule, resolveContext) do
        match reference.Module with
        | :? IAssemblyPsiModule as assemblyPsiModule ->
            let assembly = assemblyPsiModule.Assembly

            if assemblyFilter assembly then
                result.Add(assembly.Location.FullPath)

        | :? IProjectPsiModule as projectPsiModule ->
            let referencedProject = projectPsiModule.Project
            if referencedProject <> project then
                result.Add(referencedProject.GetOutputFilePath(psiModule.TargetFrameworkId).FullPath)

        | _ -> ()

    result
