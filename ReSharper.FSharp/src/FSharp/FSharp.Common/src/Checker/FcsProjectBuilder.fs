namespace JetBrains.ReSharper.Plugins.FSharp.Checker

#nowarn "57"

open System
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Compiler.CodeAnalysis
open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Threading
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ProjectModel.MSBuild
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Strategies
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.RdBackend.Common.Features.Documents
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds

[<SolutionInstanceComponent>]
[<ZoneMarker(typeof<IHostSolutionZone>)>]
type FSharpTargetsProjectLoadModificator() =
    let fsTargets =
        [| "GenerateCode"
           "GenerateFSharpInternalsVisibleToFile"
           "GenerateAssemblyFileVersionTask"
           "ImplicitlyExpandNETStandardFacades" |]

    interface MsBuildLegacyLoadStrategy.IModificator with
        member x.IsApplicable(mark) =
            match mark with
            | FSharpProjectMark -> true
            | _ -> false

        member x.ModifyTargets(targets) =
            targets.AddRange(fsTargets)

        member x.ModifyProperties _ =
            ()

module FcsProjectBuilder =
    let itemsDelimiters = [| ';'; ','; ' ' |]

    let splitAndTrim (delimiters: char[]) (s: string) =
        if isNull s then EmptyArray.Instance else
        s.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)

    let getProjectConfiguration (targetFramework: TargetFrameworkId) (project: IProject) =
        let projectProperties = project.ProjectProperties
        projectProperties.ActiveConfigurations.TryGetConfiguration(targetFramework).As<IManagedProjectConfiguration>()

    let getDefines (configuration: IManagedProjectConfiguration) =
        if isNull configuration then [] else

        splitAndTrim itemsDelimiters configuration.DefineConstants
        |> List.ofArray

[<SolutionComponent>]
[<ZoneMarker(typeof<ISinceClr4HostZone>)>]
type FcsProjectBuilder(checkerService: FcsCheckerService, itemsContainer: IFSharpItemsContainer,
        modulePathProvider: ModulePathProvider, logger: ILogger, psiModules: IPsiModules, locks: IShellLocks) =

    let defaultOptions =
        [| "--noframework"
           "--debug:full"
           "--debug+"
           "--optimize-"
           "--tailcalls-"
           "--fullpaths"
           "--highentropyva+"
           "--noconditionalerasure"
           "--ignorelinedirectives" |]

    let unusedValuesWarns =
        [| "--warnon:1182" |]

    let xmlDocsNoWarns =
        [| "--nowarn:3390" |]

    let getOutputType (outputType: ProjectOutputType) =
        match outputType with
        | ProjectOutputType.CONSOLE_EXE -> "exe"
        | ProjectOutputType.WIN_EXE -> "winexe"
        | ProjectOutputType.MODULE -> "module"
        | _ -> "library"

    abstract GetProjectItemsPaths:
        project: IProject * targetFrameworkId: TargetFrameworkId -> (VirtualFileSystemPath * BuildAction)[]

    default x.GetProjectItemsPaths(project, targetFrameworkId) =
        let projectMark = project.GetProjectMark().NotNull()
        itemsContainer.GetProjectItemsPaths(projectMark, targetFrameworkId)

    member x.GetProjectFilesAndResources(project: IProject, targetFrameworkId) =
        let sourceFiles = List()
        let resources = List()

        let sigFiles = HashSet()
        let implsWithSigs = HashSet()

        let projectItems = x.GetProjectItemsPaths(project, targetFrameworkId)

        for path, buildAction in projectItems do
            match buildAction with
            | SourceFile ->
                sourceFiles.Add(path)
                let fileName = path.NameWithoutExtension
                match path.ExtensionNoDot with
                | SigExtension -> sigFiles.Add(fileName) |> ignore
                | ImplExtension when sigFiles.Contains(fileName) -> implsWithSigs.add(path)
                | _ -> ()

            | Resource -> resources.Add(path)
            | _ -> ()

        let resources: IList<_> = if resources.IsEmpty() then EmptyList.InstanceList else resources :> _
        let implsWithSigs: ISet<_> = if implsWithSigs.IsEmpty() then EmptySet.Instance :> _ else implsWithSigs :> _

        sourceFiles.ToArray(), implsWithSigs, resources

    member x.BuildFcsProject(projectKey: FcsProjectKey): FcsProject =
        let project = projectKey.Project
        let targetFrameworkId = projectKey.TargetFrameworkId

        let projectProperties = project.ProjectProperties

        let otherOptions = List()

        let outPath = project.GetOutputFilePath(targetFrameworkId)
        if not outPath.IsEmpty then
            otherOptions.Add("--out:" + outPath.FullPath)

        otherOptions.AddRange(defaultOptions)
        otherOptions.AddRange(unusedValuesWarns)
        otherOptions.AddRange(xmlDocsNoWarns)

        match projectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg ->
            let definedConstants = FcsProjectBuilder.getDefines cfg
            otherOptions.AddRange(definedConstants |> Seq.map (fun c -> "--define:" + c))

            otherOptions.Add($"--target:{getOutputType cfg.OutputType}")

            otherOptions.Add$"--warn:{cfg.WarningLevel}"

            if cfg.TreatWarningsAsErrors then
                otherOptions.Add("--warnaserror")

            if Shell.Instance.IsTestShell then
                let psiModule = psiModules.GetPrimaryPsiModule(project, targetFrameworkId)
                let languageLevel = FSharpLanguageLevel.ofPsiModuleNoCache psiModule
                let langVersionArg =
                    languageLevel
                    |> FSharpLanguageLevel.toLanguageVersion
                    |> FSharpLanguageVersion.toCompilerArg

                otherOptions.Add(langVersionArg)

            let doc = cfg.DocumentationFile
            if not (doc.IsNullOrWhitespace()) then otherOptions.Add("--doc:" + doc)

            let props = cfg.PropertiesCollection

            let getOption f (p: string, compilerArg) =
                let compilerArg = defaultArg compilerArg (p.ToLower())
                match props.TryGetValue(p) with
                | true, v when not (v.IsNullOrWhitespace()) -> Some ("--" + compilerArg + ":" + f v)
                | _ -> None

            [ FSharpProperties.TargetProfile, None; FSharpProperties.LangVersion, None ]
            |> List.choose (getOption id)
            |> otherOptions.AddRange

            [ FSharpProperties.NoWarn, None
              MSBuildProjectUtil.WarningsAsErrorsProperty, Some("warnaserror")
              MSBuildProjectUtil.WarningsNotAsErrorsProperty, Some("warnaserror-") ]
            |> List.choose (getOption (fun v -> (FcsProjectBuilder.splitAndTrim FcsProjectBuilder.itemsDelimiters v).Join(",")))
            |> otherOptions.AddRange

            match props.TryGetValue(FSharpProperties.OtherFlags) with
            | true, otherFlags when not (otherFlags.IsNullOrWhitespace()) -> FcsProjectBuilder.splitAndTrim [| ' ' |] otherFlags
            | _ -> EmptyArray.Instance
            |> otherOptions.AddRange
        | _ -> ()

        let filePaths, implsWithSig, resources = x.GetProjectFilesAndResources(project, targetFrameworkId)

        otherOptions.AddRange(resources |> Seq.map (fun (r: VirtualFileSystemPath) -> "--resource:" + r.FullPath))
        let fileIndices = Dictionary<VirtualFileSystemPath, int>()
        Array.iteri (fun i p -> fileIndices[p] <- i) filePaths

        let psiModule = psiModules.GetPrimaryPsiModule(project, targetFrameworkId)
        
        let sourceFiles =
            psiModule.SourceFiles
            |> Seq.filter(fun psiSourceFile ->
                isNotNull psiSourceFile.PrimaryPsiLanguage
                && psiSourceFile.PrimaryPsiLanguage.Name = "F#"
            )
            |> Seq.map (fun psiSourceFile ->
                let name = psiSourceFile.GetLocation().FullPath
                (*
                
                In order to create the snapshot, we need to ensure that Resharper read lock rules are respected when getting the source.
                Today, this happens in DelegatingFileSystemShim.cs.
                So we can rely on the file system (that is shimmed) and use FSharpFileSnapshot.CreateFromFileSystem.
                
                Alternatively we can construct the snapshot via getSource:
                ```fsharp
                let version = string psiSourceFile.Document.LastModificationStamp.Value

                let getSource () =
                    task {
                        let mutable text = ""
                        FSharpAsyncUtil.UsingReadLockInsideFcs(locks, fun () ->
                            text <- psiSourceFile.Document.GetText()
                        )
                        return FSharp.Compiler.Text.SourceTextNew.ofString text
                    }

                ProjectSnapshot.FSharpFileSnapshot.Create(name, version, getSource)
                ``` 

                This also worked but for now going with the FileSystemShim seems better?
                I favour the getSource option (or a better version of it) over the FileSystemShim 
                as it makes it more explicit where the source is really coming from. 
                However, I don't have enough understanding about the plugin to really make this judgement call.

                *)
                ProjectSnapshot.FSharpFileSnapshot.CreateFromFileSystem(name)
            )
            |> Seq.toList
        
        let references = projectKey.Project.GetModuleReferences(projectKey.TargetFrameworkId)
        let referencesOnDisk: ProjectSnapshot.ReferenceOnDisk list =
            references
            |> Seq.choose (fun projectToModuleReference ->
                projectToModuleReference
                |> modulePathProvider.GetModulePath
                |> Option.bind (fun path ->
                    if path.IsEmpty then
                        None
                    else
                        Some ({
                            Path = path.FullPath
                            LastModified = if path.ExistsFile then path.FileModificationTimeUtc else DateTime.MinValue
                        } : ProjectSnapshot.ReferenceOnDisk))
            )
            |> Seq.toList

        let otherOptions = Seq.toList otherOptions
        
        let projectSnapshot =
            FSharpProjectSnapshot.Create(
                projectFileName = $"{project.ProjectFileLocation}.{targetFrameworkId}.fsproj",
                projectId = None,
                sourceFiles = sourceFiles,
                referencesOnDisk = referencesOnDisk,
                otherOptions = Seq.toList otherOptions,
                referencedProjects = List.empty,
                isIncompleteTypeCheckEnvironment = false,
                useScriptResolutionRules = false,
                loadTime = DateTime.Now,
                unresolvedReferences = None,
                originalLoadReferences = List.empty,
                stamp = None
            )

        let parsingOptions, errors =
            checkerService.Checker.GetParsingOptionsFromCommandLineArgs(otherOptions)

        let defines = ImplicitDefines.sourceDefines @ parsingOptions.ConditionalDefines

        let parsingOptions = { parsingOptions with
                                 SourceFiles = sourceFiles |> List.map (fun sf -> sf.FileName) |> Array.ofList
                                 ConditionalDefines = defines }

        if not errors.IsEmpty then
            logger.Warn("Getting parsing options: {0}", concatErrors errors)

        let fcsProject =
            { OutputPath = outPath
              ProjectSnapshot = projectSnapshot 
              ParsingOptions = parsingOptions
              FileIndices = fileIndices
              ImplementationFilesWithSignatures = implsWithSig
              ReferencedModules = HashSet() }

        fcsProject
