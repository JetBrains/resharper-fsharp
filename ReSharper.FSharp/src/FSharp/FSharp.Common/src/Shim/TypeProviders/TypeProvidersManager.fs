namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open System.Collections.Concurrent
open System.Collections.Generic
open FSharp.Compiler.TypeProviders
open FSharp.Compiler.Text
open FSharp.Core.CompilerServices
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Build
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
open JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter
open JetBrains.Rd.Tasks
open JetBrains.Rider.FSharp.TypeProviders.Protocol.Client
open JetBrains.Util.Concurrency

type internal TypeProvidersCache() =
    let typeProvidersPerAssembly = ConcurrentDictionary<string, ConcurrentDictionary<int, IProxyTypeProvider>>()
    let proxyTypeProvidersPerId = ConcurrentDictionary<int, IProxyTypeProvider>()

    let rec addTypeProvider (projectAssembly: string) (tp: IProxyTypeProvider) =
        match typeProvidersPerAssembly.TryGetValue(projectAssembly) with
        | true, assemblyCache ->
            match assemblyCache.TryGetValue(tp.EntityId) with
            | true, _ -> ()
            | false, _ ->
                assemblyCache.TryAdd(tp.EntityId, tp) |> ignore
                proxyTypeProvidersPerId.TryAdd(tp.EntityId, tp) |> ignore
                tp.Disposed.Add(fun _ -> removeTypeProvider projectAssembly tp.EntityId)
        | false, _ ->
            typeProvidersPerAssembly.TryAdd(projectAssembly, ConcurrentDictionary()) |> ignore
            addTypeProvider projectAssembly tp

    and removeTypeProvider projectAssembly tpId =
        typeProvidersPerAssembly[projectAssembly].TryRemove(tpId) |> ignore
        proxyTypeProvidersPerId.TryRemove(tpId) |> ignore

        if typeProvidersPerAssembly[projectAssembly].Count = 0 then
            typeProvidersPerAssembly.TryRemove(projectAssembly) |> ignore

    member x.Add(projectAssembly, tp) =
        addTypeProvider projectAssembly tp

    member x.TryGet(id) = proxyTypeProvidersPerId.TryGetValue(id)

    member x.Get(id) =
        match proxyTypeProvidersPerId.TryGetValue(id) with
        | true, provider -> provider
        | _ -> Assertion.Fail($"Cannot get type provider {id} from TypeProvidersCache"); null

    member x.Get(projectOutputPath) =
        match typeProvidersPerAssembly.TryGetValue(projectOutputPath) with
        | true, x -> x.Values
        | _ -> [||]

    member x.Dump() =
        let typeProviders =
            proxyTypeProvidersPerId
            |> Seq.map (fun t -> t.Key, t.Value.GetDisplayName(true))
            |> Seq.sortBy snd
            |> Seq.map (fun (id, name) -> $"{id} {name}")
            |> String.concat "\n"

        $"Type Providers:\n{typeProviders}"


type IProxyTypeProvidersManager =
    abstract member GetOrCreate:
        runTimeAssemblyFileName: string *
        designTimeAssemblyNameString: string *
        resolutionEnvironment: ResolutionEnvironment *
        isInvalidationSupported: bool *
        isInteractive: bool *
        systemRuntimeContainsType: (string -> bool) *
        systemRuntimeAssemblyVersion: Version *
        compilerToolsPath: string list *
        m: range -> ITypeProvider list

    abstract member Context: TypeProvidersContext
    abstract member HasGenerativeTypeProviders: project: IProject -> bool
    abstract member Dump: unit -> string

type TypeProvidersManager(connection: TypeProvidersConnection, fcsProjectProvider: IFcsProjectProvider,
                          scriptPsiModulesProvider: FSharpScriptPsiModulesProvider,
                          outputAssemblies: OutputAssemblies, enableGenerativeTypeProvidersInMemoryAnalysis) =
    let protocol = connection.ProtocolModel.RdTypeProviderProcessModel
    let lifetime = connection.Lifetime
    let tpContext = TypeProvidersContext(connection, enableGenerativeTypeProvidersInMemoryAnalysis)
    let typeProviders = TypeProvidersCache()
    let lock = SpinWaitLockRef()
    let projectsWithGenerativeProviders = HashSet<string>()

    let addProjectWithGenerativeProvider outputPath =
        let outputAssemblyPath = VirtualFileSystemPath.Parse(outputPath, InteractionContext.SolutionContext)
        Assertion.Assert(not outputAssemblyPath.IsEmpty, "OutputAssemblyPath expected to be not empty")
        match outputAssemblies.TryGetProjectByOutputAssemblyLocation(outputAssemblyPath) with
        | null -> ()
        | project ->
            use lock = lock.Push()
            projectsWithGenerativeProviders.Add(project.GetPersistentID()) |> ignore

    let disposeTypeProviders (path: string) =
        let providersToDispose = typeProviders.Get(path)
        if providersToDispose.Count = 0 then () else

        let providersIds = [| for tp in providersToDispose -> tp.EntityId |]
        connection.Execute(fun () -> protocol.Dispose.Start(lifetime, providersIds)) |> ignore

        for typeProvider in providersToDispose do typeProvider.DisposeProxy()

        Assertion.Assert(typeProviders.Get(path) |> Seq.isEmpty, "Type Providers should be disposed")

    do
        connection.Execute(fun () ->
            protocol.Invalidate.Advise(lifetime, fun id ->
                match typeProviders.TryGet(id) with
                | true, provider -> provider.OnInvalidate()
                | _ -> ()))

        fcsProjectProvider.ProjectRemoved.Advise(lifetime, fun (projectKey, fcsProject) ->
            use lock = lock.Push()
            let project = projectKey.Project
            projectsWithGenerativeProviders.Remove(project.GetPersistentID()) |> ignore
            disposeTypeProviders fcsProject.OutputPath.FullPath
        )

        scriptPsiModulesProvider.ModuleInvalidated.Advise(lifetime,
            fun psiModule -> disposeTypeProviders psiModule.Path.FullPath)

    interface IProxyTypeProvidersManager with
        member x.GetOrCreate(runTimeAssemblyFileName: string, designTimeAssemblyNameString: string,
                resolutionEnvironment: ResolutionEnvironment, isInvalidationSupported: bool, isInteractive: bool,
                systemRuntimeContainsType: string -> bool, systemRuntimeAssemblyVersion: Version,
                compilerToolsPath: string list, m: range) =

            let envPath, projectPsiModule =
                match resolutionEnvironment.OutputFile with
                | Some file ->
                    // todo: module might have been changed after project changes
                    file, fcsProjectProvider.GetPsiModule(VirtualFileSystemPath.Parse(file, InteractionContext.SolutionContext))
                | None -> m.FileName, None

            let fakeTcImports = TcImportsHack.GetFakeTcImports(systemRuntimeContainsType)

            let typeProviderProxies =
                connection.ExecuteWithCatch(fun () ->
                    let result = protocol.InstantiateTypeProvidersOfAssembly.Sync(
                        InstantiateTypeProvidersOfAssemblyParameters(runTimeAssemblyFileName,
                            designTimeAssemblyNameString, resolutionEnvironment.toRdResolutionEnvironment(),
                            isInvalidationSupported, isInteractive, systemRuntimeAssemblyVersion.ToString(),
                            compilerToolsPath |> Array.ofList, fakeTcImports, envPath), RpcTimeouts.Maximal)

                    [ for tp in result.TypeProviders ->
                         let tp = new ProxyTypeProvider(tp, tpContext, Option.toObj projectPsiModule)

                         if not isInteractive then
                             tp.ContainsGenerativeTypes.Add(fun _ -> addProjectWithGenerativeProvider envPath)

                         typeProviders.Add(envPath, tp)
                         tp :> ITypeProvider

                      for id in result.CachedIds ->
                         typeProviders.Get(id) :> ITypeProvider ])

            typeProviderProxies

        member this.Context = tpContext

        member this.HasGenerativeTypeProviders(project) =
            use lock = lock.Push()
            projectsWithGenerativeProviders.Contains(project.GetPersistentID())

        member this.Dump() =
            $"{typeProviders.Dump()}\n\n{tpContext.Dump()}"
