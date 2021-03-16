namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open System.Collections.Generic
open FSharp.Compiler.ExtensionTyping
open FSharp.Core.CompilerServices
open JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders.TcImportsHack
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Exceptions
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
open JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter
open JetBrains.Rd.Tasks
open JetBrains.Rider.FSharp.TypeProvidersProtocol.Server

type internal TypeProvidersCache() =
    let typeProvidersPerAssembly = Dictionary<_, Dictionary<_, IProxyTypeProvider>>()
    let proxyTypeProvidersPerId = Dictionary<_, IProxyTypeProvider>()

    let rec addTypeProvider envKey (tp: IProxyTypeProvider) =
        let fullName = tp.GetDisplayName(fullName = true)

        match typeProvidersPerAssembly.TryGetValue(envKey) with
        | true, assemblyCache ->
            match assemblyCache.TryGetValue(fullName) with
            | true, oldTp ->
                oldTp.Dispose()
                addTypeProvider envKey tp
            | false, _ ->
                assemblyCache.Add(fullName, tp)
                proxyTypeProvidersPerId.Add(tp.EntityId, tp)
                tp.Disposed.Add(fun _ -> removeTypeProvider envKey tp)
        | false, _ ->
            typeProvidersPerAssembly.Add(envKey, Dictionary())
            addTypeProvider envKey tp

    and removeTypeProvider envKey (tp: IProxyTypeProvider) =
        // Removes types in a unified manner, may also be disposed by FCS.
        typeProvidersPerAssembly.[envKey].Remove(tp.GetDisplayName(true)) |> ignore
        proxyTypeProvidersPerId.Remove(tp.EntityId) |> ignore

        if typeProvidersPerAssembly.[envKey].Count = 0 then
            typeProvidersPerAssembly.Remove(envKey) |> ignore

    member x.Add(envKey, tp) =
        addTypeProvider envKey tp

    member x.Get(id) =
        proxyTypeProvidersPerId.[id]

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

    abstract member Dump: unit -> string

type TypeProvidersManager(connection: TypeProvidersConnection) =
    let protocol = connection.ProtocolModel.RdTypeProviderProcessModel
    let lifetime = connection.Lifetime
    let tpContext = TypeProvidersContext(connection)
    let typeProviders = TypeProvidersCache()

    do connection.Execute(fun () ->
        protocol.Invalidate.Advise(lifetime, fun id -> typeProviders.Get(id).OnInvalidate()))

    interface IProxyTypeProvidersManager with
        member x.GetOrCreate(runTimeAssemblyFileName: string, designTimeAssemblyNameString: string,
                resolutionEnvironment: ResolutionEnvironment, isInvalidationSupported: bool, isInteractive: bool,
                systemRuntimeContainsType: string -> bool, systemRuntimeAssemblyVersion: Version,
                compilerToolsPath: string list) =
            let envKey = $"{designTimeAssemblyNameString}+{resolutionEnvironment.resolutionFolder}"

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
                     typeProviders.Add(envKey, tp)
                     tp :> ITypeProvider

                  for id in result.CachedIds ->
                     let tp = typeProviders.Get(id)
                     tp.IncrementVersion()
                     tp :> ITypeProvider ]

            typeProviderProxies

        member this.Dump() =
            $"{typeProviders.Dump()}\n\n{tpContext.Dump()}"
