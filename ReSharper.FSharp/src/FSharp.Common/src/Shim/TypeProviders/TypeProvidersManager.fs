namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Threading
open FSharp.Compiler.ExtensionTyping
open FSharp.Core.CompilerServices
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders.TcImportsHack
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
open JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter
open JetBrains.Rd.Tasks
open JetBrains.Rider.FSharp.TypeProviders.Protocol.Client
open JetBrains.Threading

type internal TypeProvidersCache() =
    let typeProvidersPerAssembly = ConcurrentDictionary<_, ConcurrentDictionary<_, IProxyTypeProvider>>()
    let proxyTypeProvidersPerId = ConcurrentDictionary<_, _>()

    let rec addTypeProvider projectAssembly tpAssembly (tp: IProxyTypeProvider) =
        let tpKey = struct(tpAssembly, tp.GetDisplayName(fullName = true))

        match typeProvidersPerAssembly.TryGetValue(projectAssembly) with
        | true, assemblyCache ->
            match assemblyCache.TryGetValue(tpKey) with
            | true, oldTp ->
                oldTp.Dispose()
                addTypeProvider projectAssembly tpAssembly tp
            | false, _ ->
                assemblyCache.TryAdd(tpKey, tp) |> ignore
                proxyTypeProvidersPerId.TryAdd(tp.EntityId, tp) |> ignore
                tp.Disposed.Add(fun _ -> removeTypeProvider projectAssembly tpKey tp.EntityId)
        | false, _ ->
            typeProvidersPerAssembly.TryAdd(projectAssembly, ConcurrentDictionary()) |> ignore
            addTypeProvider projectAssembly tpAssembly tp

    and removeTypeProvider projectAssembly tpKey tpId =
        // Removes types in a unified manner, may also be disposed by FCS.
        typeProvidersPerAssembly.[projectAssembly].TryRemove(tpKey) |> ignore
        proxyTypeProvidersPerId.TryRemove(tpId) |> ignore

        if typeProvidersPerAssembly.[projectAssembly].Count = 0 then
            typeProvidersPerAssembly.TryRemove(projectAssembly) |> ignore

    member x.Add(projectAssembly, tpAssembly, tp) =
        addTypeProvider projectAssembly tpAssembly tp

    member x.Get(id) =
        let hasValue = SpinWait.SpinUntil((fun () -> proxyTypeProvidersPerId.ContainsKey id), 15_000)

        if not hasValue then failwith $"Cannot get type provider {id} from TypeProvidersCache"
        else proxyTypeProvidersPerId.[id]

    member x.Get(outputPath) =
        match typeProvidersPerAssembly.TryGetValue(outputPath) with
        | true, x -> x.Values
        | false, _ -> [||] :> _

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
        compilerToolsPath: string list -> ITypeProvider list

    abstract member HasGenerativeTypeProviders: project: IProject -> bool
    abstract member Dump: unit -> string

type TypeProvidersManager(connection: TypeProvidersConnection, fcsProjectProvider: IFcsProjectProvider) =
    let protocol = connection.ProtocolModel.RdTypeProviderProcessModel
    let lifetime = connection.Lifetime
    let tpContext = TypeProvidersContext(connection)
    let typeProviders = TypeProvidersCache()
    let lock = SpinWaitLock()
    let doesProjectContainGenerativeProviders = Dictionary<IProject, bool>()

    do
        connection.Execute(fun () ->
            protocol.Invalidate.Advise(lifetime, fun id -> typeProviders.Get(id).OnInvalidate()))

        fcsProjectProvider.ModuleInvalidated.Advise(lifetime, fun psiModule ->
            let project = getModuleProject psiModule
            try lock.Enter()
                doesProjectContainGenerativeProviders.Remove(project.NotNull()) |> ignore
            finally lock.Exit())

    interface IProxyTypeProvidersManager with
        member x.GetOrCreate(runTimeAssemblyFileName: string, designTimeAssemblyNameString: string,
                resolutionEnvironment: ResolutionEnvironment, isInvalidationSupported: bool, isInteractive: bool,
                systemRuntimeContainsType: string -> bool, systemRuntimeAssemblyVersion: Version,
                compilerToolsPath: string list) =
            let projectAssembly = resolutionEnvironment.outputFile.Value

            let result =
                let fakeTcImports = getFakeTcImports systemRuntimeContainsType

                connection.ExecuteWithCatch(fun () ->
                    protocol.InstantiateTypeProvidersOfAssembly.Sync(
                        InstantiateTypeProvidersOfAssemblyParameters(runTimeAssemblyFileName,
                            designTimeAssemblyNameString, resolutionEnvironment.toRdResolutionEnvironment(),
                            isInvalidationSupported, isInteractive, systemRuntimeAssemblyVersion.ToString(),
                            compilerToolsPath |> Array.ofList, fakeTcImports), RpcTimeouts.Maximal))

            let typeProviderProxies =
                [ for tp in result.TypeProviders ->
                     let tp = new ProxyTypeProvider(tp, tpContext)
                     typeProviders.Add(projectAssembly, designTimeAssemblyNameString, tp)
                     tp :> ITypeProvider

                  for id in result.CachedIds ->
                     let tp = typeProviders.Get(id)
                     tp.IncrementVersion()
                     tp :> ITypeProvider ]

            typeProviderProxies

        member this.HasGenerativeTypeProviders(project) =
            try lock.Enter()
                match doesProjectContainGenerativeProviders.TryGetValue(project) with
                | true, has -> has
                | _ ->
                    let has =
                        fcsProjectProvider.GetProjectOutputPaths(project)
                        |> Seq.collect typeProviders.Get
                        |> Seq.exists (fun x -> x.IsGenerative)
                    doesProjectContainGenerativeProviders.[project] <- has
                    has
            finally lock.Exit()

        member this.Dump() =
            $"{typeProviders.Dump()}\n\n{tpContext.Dump()}"
