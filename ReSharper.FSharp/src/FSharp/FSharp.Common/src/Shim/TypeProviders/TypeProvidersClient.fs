namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open System.Collections.Concurrent
open System.Collections.Generic
open FSharp.Compiler.TypeProviders
open FSharp.Compiler.Text
open FSharp.Core.CompilerServices
open JetBrains.Diagnostics
open JetBrains.Lifetimes
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
    let typeProvidersPerScriptOrProjectOutputPath = ConcurrentDictionary<string, ConcurrentDictionary<int, IProxyTypeProvider>>()
    let proxyTypeProvidersPerId = ConcurrentDictionary<int, IProxyTypeProvider>()

    let rec addTypeProvider (scriptOrProjectOutputPath: string) (tp: IProxyTypeProvider) =
        match typeProvidersPerScriptOrProjectOutputPath.TryGetValue(scriptOrProjectOutputPath) with
        | true, assemblyCache ->
            match assemblyCache.TryGetValue(tp.EntityId) with
            | true, _ -> ()
            | false, _ ->
                assemblyCache.TryAdd(tp.EntityId, tp) |> ignore
                proxyTypeProvidersPerId.TryAdd(tp.EntityId, tp) |> ignore
                tp.Disposed.Add(fun _ -> removeTypeProvider scriptOrProjectOutputPath tp.EntityId)
        | false, _ ->
            typeProvidersPerScriptOrProjectOutputPath.TryAdd(scriptOrProjectOutputPath, ConcurrentDictionary()) |> ignore
            addTypeProvider scriptOrProjectOutputPath tp

    and removeTypeProvider scriptOrProjectOutputPath tpId =
        typeProvidersPerScriptOrProjectOutputPath[scriptOrProjectOutputPath].TryRemove(tpId) |> ignore
        proxyTypeProvidersPerId.TryRemove(tpId) |> ignore

        if typeProvidersPerScriptOrProjectOutputPath[scriptOrProjectOutputPath].Count = 0 then
            typeProvidersPerScriptOrProjectOutputPath.TryRemove(scriptOrProjectOutputPath) |> ignore

    member x.Add(scriptOrProjectOutputPath, tp) =
        addTypeProvider scriptOrProjectOutputPath tp

    member x.TryGet(id) = proxyTypeProvidersPerId.TryGetValue(id)

    member x.Get(id) =
        match proxyTypeProvidersPerId.TryGetValue(id) with
        | true, provider -> provider
        | _ -> Assertion.Fail($"Cannot get type provider {id} from TypeProvidersCache"); null

    member x.Get(projectOutputPath) =
        match typeProvidersPerScriptOrProjectOutputPath.TryGetValue(projectOutputPath) with
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

type TypeProvidersHostingScope =
    | Solution
    | Scripts

type ITypeProvidersClient =
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
    abstract member IsActive: bool
    abstract member Execute: (TypeProvidersConnection -> 'a) -> 'a
    abstract member Terminate: unit -> unit
    abstract member Dump: unit -> string

[<AbstractClass>]
type internal TypeProvidersClientBase(lifetimeDef: LifetimeDefinition, connection: TypeProvidersConnection,
                                      enableGenerativeTypeProvidersInMemoryAnalysis) =
    let protocol = connection.ProtocolModel.RdTypeProviderProcessModel
    let lifetime = connection.Lifetime
    let tpContext = TypeProvidersContext(connection, enableGenerativeTypeProvidersInMemoryAnalysis)
    let typeProviders = TypeProvidersCache()

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

    member this.Lifetime = lifetime
    member this.DisposeTypeProviders(path: string) = disposeTypeProviders path
    abstract member CreateTypeProviders: RdTypeProvider[] * TypeProvidersContext * ResolutionEnvironment -> IProxyTypeProvider list

    interface ITypeProvidersClient with
        member x.GetOrCreate(runTimeAssemblyFileName: string, designTimeAssemblyNameString: string,
                resolutionEnvironment: ResolutionEnvironment, isInvalidationSupported: bool, isInteractive: bool,
                systemRuntimeContainsType: string -> bool, systemRuntimeAssemblyVersion: Version,
                compilerToolsPath: string list, m: range) =

            let envPath =
                match resolutionEnvironment.OutputFile with
                | Some file -> file
                | None -> m.FileName

            let fakeTcImports = TcImportsHack.GetFakeTcImports(systemRuntimeContainsType)

            let typeProviderProxies =
                connection.ExecuteWithCatch(fun () ->
                    let result = protocol.InstantiateTypeProvidersOfAssembly.Sync(
                        InstantiateTypeProvidersOfAssemblyParameters(runTimeAssemblyFileName,
                            designTimeAssemblyNameString, resolutionEnvironment.toRdResolutionEnvironment(),
                            isInvalidationSupported, isInteractive, systemRuntimeAssemblyVersion.ToString(),
                            compilerToolsPath |> Array.ofList, fakeTcImports, envPath), RpcTimeouts.Maximal)

                    [ for tp in x.CreateTypeProviders(result.TypeProviders, tpContext, resolutionEnvironment) ->
                         typeProviders.Add(envPath, tp)
                         tp :> ITypeProvider

                      for id in result.CachedIds ->
                         typeProviders.Get(id) ])

            typeProviderProxies

        member this.Context = tpContext
        member this.IsActive = connection.IsActive
        member this.Execute(action) = connection.Execute(fun _ -> action(connection))
        member this.Terminate() = lifetimeDef.Terminate()

        member this.Dump() =
            $"{typeProviders.Dump()}\n\n{tpContext.Dump()}"


type internal SolutionTypeProvidersClient(lifetimeDef: LifetimeDefinition,
                                          connection: TypeProvidersConnection,
                                          fcsProjectProvider: IFcsProjectProvider,
                                          outputAssemblies: OutputAssemblies,
                                          enableGenerativeTypeProvidersInMemoryAnalysis) as this =
    inherit TypeProvidersClientBase(lifetimeDef, connection, enableGenerativeTypeProvidersInMemoryAnalysis)

    let lock = SpinWaitLockRef()
    let projectsWithGenerativeProviders = HashSet<string>()

    do
        fcsProjectProvider.ProjectRemoved.Advise(this.Lifetime, fun (projectKey, fcsProject) ->
            use lock = lock.Push()
            let project = projectKey.Project
            projectsWithGenerativeProviders.Remove(project.GetPersistentID()) |> ignore
            this.DisposeTypeProviders(fcsProject.OutputPath.FullPath)
        )

    let addProjectWithGenerativeProvider outputPath =
        let outputAssemblyPath = VirtualFileSystemPath.Parse(outputPath, InteractionContext.SolutionContext)
        Assertion.Assert(not outputAssemblyPath.IsEmpty, "OutputAssemblyPath expected to be not empty")
        match outputAssemblies.TryGetProjectByOutputAssemblyLocation(outputAssemblyPath) with
        | null -> ()
        | project ->
            use lock = lock.Push()
            projectsWithGenerativeProviders.Add(project.GetPersistentID()) |> ignore

    override this.CreateTypeProviders(tps, context, resolutionEnv) =
        let psiModule =
            resolutionEnv.OutputFile
            |> Option.bind (fun file -> fcsProjectProvider.GetPsiModule(VirtualFileSystemPath.Parse(file, InteractionContext.SolutionContext)))
            |> Option.toObj

        let typeProviders = [ for tp in tps -> new ProxyTypeProvider(tp, context, psiModule) :> IProxyTypeProvider ]
        match resolutionEnv.OutputFile with
        | None -> ()
        | Some outputFile ->
            for tp in typeProviders do
                tp.ContainsGenerativeTypes.Add(fun _ -> addProjectWithGenerativeProvider outputFile)

        typeProviders

    member this.HasGenerativeTypeProviders(project: IProject) =
        use lock = lock.Push()
        projectsWithGenerativeProviders.Contains(project.GetPersistentID())


type internal ScriptTypeProvidersClient(lifetimeDef: LifetimeDefinition, connection: TypeProvidersConnection,
                                        scriptPsiModulesProvider: FSharpScriptPsiModulesProvider) as this =
    inherit TypeProvidersClientBase(lifetimeDef, connection, false)

    do
        scriptPsiModulesProvider.ModuleInvalidated.Advise(this.Lifetime, fun psiModule ->
            this.DisposeTypeProviders(psiModule.Path.FullPath))

    override this.CreateTypeProviders(tps, context, _) =
        [ for tp in tps -> new ProxyTypeProvider(tp, context, null) ]
