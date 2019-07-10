namespace rec JetBrains.ReSharper.Plugins.FSharp.Checker

open System
open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Text
open JetBrains.Annotations
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Assemblies.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.Threading
open JetBrains.Util

[<SolutionComponent>]
type FSharpProjectOptionsProvider
        (lifetime, solution: ISolution, changeManager: ChangeManager, checkerService: FSharpCheckerService,
         optionsBuilder: IFSharpProjectOptionsBuilder, scriptOptionsProvider: FSharpScriptOptionsProvider,
         fsFileService: IFSharpFileService, moduleReferenceResolveStore: ModuleReferencesResolveStore, logger: ILogger,
         psiModules: IPsiModules, resolveContextManager: PsiModuleResolveContextManager) as this =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    let [<Literal>] invalidatingProjectChangeType =
        ProjectModelChangeType.PROPERTIES ||| ProjectModelChangeType.TARGET_FRAMEWORK |||
        ProjectModelChangeType.REFERENCE_TARGET

    let [<Literal>] invalidatingChildChangeType =
        ProjectModelChangeType.ADDED ||| ProjectModelChangeType.REMOVED |||
        ProjectModelChangeType.MOVED_IN ||| ProjectModelChangeType.MOVED_OUT |||
        ProjectModelChangeType.REFERENCE_TARGET

    let projects = Dictionary<IProject, Dictionary<IPsiModule, FSharpProject>>()
    let psiModulesToFsProjects = Dictionary<IPsiModule, FSharpProject>()

    let locker = JetFastSemiReenterableRWLock()
    do
        changeManager.Changed2.Advise(lifetime, this.ProcessChange)
        checkerService.OptionsProvider <- this
        lifetime.OnTermination(fun _ -> checkerService.OptionsProvider <- Unchecked.defaultof<_>) |> ignore

    let moduleInvalidated = new Signal<IPsiModule>(lifetime, "FSharpPsiModuleInvalidated")

    let tryGetFSharpProject (psiModule: IPsiModule) =
        use lock = locker.UsingReadLock()
        tryGetValue psiModule psiModulesToFsProjects

    let rec createFSharpProject (project: IProject) (psiModule: IPsiModule) =
        let mutable fsProject = Unchecked.defaultof<_>
        match psiModulesToFsProjects.TryGetValue(psiModule, &fsProject) with
        | true -> fsProject
        | _ ->

        logger.Info("Creating options for {0} {1}", project, psiModule)
        let fsProject = optionsBuilder.BuildSingleFSharpProject(project, psiModule)

        let referencedProjectsOptions = seq {
            let resolveContext =
                resolveContextManager.GetOrCreateModuleResolveContext(project, psiModule, psiModule.TargetFrameworkId)

            let referencedProjectsPsiModules =
                psiModules.GetModuleReferences(psiModule, resolveContext)
                |> Seq.choose (fun reference ->
                    match reference.Module.ContainingProjectModule with
                    | FSharpProject referencedProject when
                            referencedProject.IsOpened && (referencedProject != project) ->
                        Some reference.Module
                    | _ -> None)

            for referencedPsiModule in referencedProjectsPsiModules do
                let project = referencedPsiModule.ContainingProjectModule :?> IProject
                let outPath = project.GetOutputFilePath(referencedPsiModule.TargetFrameworkId).FullPath
                let fsProject = createFSharpProject project referencedPsiModule
                yield outPath, fsProject.ProjectOptions }

        let options = { fsProject.ProjectOptions with ReferencedProjects = Array.ofSeq referencedProjectsOptions }
        let fsProject = { fsProject with ProjectOptions = options }

        psiModulesToFsProjects.[psiModule] <- fsProject
        projects.GetOrCreateValue(project, fun () -> Dictionary()).[psiModule] <- fsProject

        fsProject

    let getOrCreateFSharpProject (file: IPsiSourceFile) =
        match tryGetFSharpProject file.PsiModule with
        | Some _ as result -> result
        | _ ->

        match file.GetProject() with
        | FSharpProject project ->
            use lock = locker.UsingWriteLock()
            let fsProject = createFSharpProject project file.PsiModule
            Some fsProject

        | _ -> None

    let invalidateProject project =
        let invalidatedProjects = HashSet()
        let rec invalidate (project: IProject) =
            logger.Info("Invalidating {0}", project)
            match tryGetValue project projects with
            | None -> ()
            | Some fsProjectsForProject ->
                for KeyValue (psiModule, fsProject) in fsProjectsForProject do
                    checkerService.InvalidateFSharpProject(fsProject)
                    moduleInvalidated.Fire(psiModule)
                    psiModulesToFsProjects.Remove(psiModule) |> ignore

                fsProjectsForProject.Clear()

            invalidatedProjects.Add(project) |> ignore
            // todo: keep referencing projects for invalidating removed projects
            let referencesToProject = moduleReferenceResolveStore.GetReferencesToProject(project)
            if not (referencesToProject.IsEmpty()) then
                logger.Info("Invalidating projects reverencing {0}", project)
                for reference in referencesToProject do
                    match reference.GetProject() with
                    | FSharpProject referencingProject when
                            not (invalidatedProjects.Contains(referencingProject)) ->
                        invalidate referencingProject
                    | _ -> ()
                logger.Info("Done invalidating {0}", project)
        invalidate project


    let isScriptLike file =
        fsFileService.IsScriptLike(file) || file.PsiModule.IsMiscFilesProjectModule() || isNull (file.GetProject())        

    let getParsingOptionsForSingleFile ([<NotNull>] sourceFile: IPsiSourceFile) isScript =
        { FSharpParsingOptions.Default with
            SourceFiles = [| sourceFile.GetLocation().FullPath |]
            IsExe = isScript }

    member x.ModuleInvalidated = moduleInvalidated

    member private x.ProcessChange(obj: ChangeEventArgs) =
        match obj.ChangeMap.GetChange<ProjectModelChange>(solution) with
        | null -> ()

        | :? ProjectReferenceChange as referenceChange ->
            use lock = locker.UsingWriteLock()
            let referenceProject = referenceChange.ProjectToModuleReference.OwnerModule
            if referenceProject.IsFSharp then
                invalidateProject referenceProject

        | change ->
            if not change.IsClosingSolution then
                use lock = locker.UsingWriteLock()
                x.VisitDelta(change)

    override x.VisitDelta(change: ProjectModelChange) =
        match change.ProjectModelElement with
        | :? IProject as project ->
            if project.IsFSharp then
                if change.ContainsChangeType(invalidatingProjectChangeType) then
                    invalidateProject project

                else if change.IsSubtreeChanged then
                    let mutable shouldInvalidate = false
                    let changeVisitor =
                        { new RecursiveProjectModelChangeDeltaVisitor() with
                            member x.VisitDelta(change) =
                                if change.ContainsChangeType(invalidatingChildChangeType) then
                                    shouldInvalidate <- true

                                if not shouldInvalidate then
                                    base.VisitDelta(change) }

                    change.Accept(changeVisitor)
                    if shouldInvalidate then
                        invalidateProject project
    
                if change.IsRemoved then
                    solution.GetComponent<FSharpItemsContainer>().RemoveProject(project)

                    let mutable fsProjects = Unchecked.defaultof<_>
                    match projects.TryGetValue(project, &fsProjects) with
                    | false -> ()
                    | _ ->

                    logger.Info("Removing {0}", project)

                    for KeyValue (psiModule, fsProject) in fsProjects do
                        checkerService.InvalidateFSharpProject(fsProject)
                        psiModulesToFsProjects.Remove(psiModule) |> ignore

                    projects.Remove(project) |> ignore

            else if project.ProjectProperties.ProjectKind = ProjectKind.SOLUTION_FOLDER then
                base.VisitDelta(change)

        | :? ISolution -> base.VisitDelta(change)
        | _ -> ()

    interface IFSharpProjectOptionsProvider with
        member x.GetProjectOptions(file) =
            if fsFileService.IsScriptLike(file) then scriptOptionsProvider.GetScriptOptions(file) else
            if file.PsiModule.IsMiscFilesProjectModule() then None else

            getOrCreateFSharpProject file
            |> Option.map (fun fsProject -> fsProject.ProjectOptions)

        member x.HasPairFile(file) =
            if isScriptLike file then false else

            getOrCreateFSharpProject file
            |> Option.map (fun fsProject -> fsProject.ImplFilesWithSigs.Contains(file.GetLocation()))
            |> Option.defaultValue false

        member x.GetParsingOptions(sourceFile) =
            if isScriptLike sourceFile then getParsingOptionsForSingleFile sourceFile true else

            getOrCreateFSharpProject sourceFile
            |> Option.map (fun fsProject -> fsProject.ParsingOptions)
            |> Option.defaultWith (fun _ -> getParsingOptionsForSingleFile sourceFile false)

        member x.GetFileIndex(sourceFile) =
            if isScriptLike sourceFile then 0 else

            getOrCreateFSharpProject sourceFile
            |> Option.bind (fun fsProject ->
                let path = sourceFile.GetLocation()
                tryGetValue path fsProject.FileIndices)
            |> Option.defaultWith (fun _ -> -1)
        
        member x.ModuleInvalidated = x.ModuleInvalidated :> _

[<SolutionComponent>]
type FSharpScriptOptionsProvider(logger: ILogger, checkerService: FSharpCheckerService) =
    let getScriptOptionsLock = obj()
    let otherFlags = [| "--warnon:1182" |]

    member x.GetScriptOptions(file: IPsiSourceFile) =
        let filePath = file.GetLocation().FullPath
        let source = SourceText.ofString (file.Document.GetText())
        lock getScriptOptionsLock (fun _ ->
        let getScriptOptionsAsync =
            checkerService.Checker.GetProjectOptionsFromScript(filePath, source, otherFlags = otherFlags)
        try
            let options, errors = getScriptOptionsAsync.RunAsTask()
            if not errors.IsEmpty then
                logger.Warn("Script options for {0}: {1}", filePath, concatErrors errors)
            let options = x.FixScriptOptions(options)
            Some options
        with
        | :? OperationCanceledException -> reraise()
        | exn ->
            logger.Warn("Error while getting script options for {0}: {1}", filePath, exn.Message)
            logger.LogExceptionSilently(exn)
            None)

    member private x.FixScriptOptions(options) =
        { options with OtherOptions = FSharpCoreFix.ensureCorrectFSharpCore options.OtherOptions }
