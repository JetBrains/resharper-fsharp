namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open System.Collections.Concurrent
open FSharp.Compiler
open FSharp.Compiler.TypeProviders
open FSharp.Compiler.Text
open FSharp.Core.CompilerServices
open JetBrains
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.Application.Parts
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Build
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models

type ITypeProvidersShim =
    inherit IExtensionTypingProvider

    abstract RuntimeVersion: unit -> string
    abstract HasGenerativeTypeProviders: project: IProject -> bool
    abstract DumpTypeProvidersProcess: unit -> string
    abstract SolutionTypeProvidersClient: ITypeProvidersClient


[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type TypeProvidersShim(solution: ISolution, toolset: ISolutionToolset,
                       experimentalFeatures: FSharpExperimentalFeaturesProvider,
                       fcsProjectProvider: IFcsProjectProvider,
                       scriptPsiModulesProvider: FSharpScriptPsiModulesProvider,
                       outputAssemblies: OutputAssemblies,
                       typeProvidersProcessFactory: TypeProvidersExternalProcessFactory,
                       productConfigurations: RunsProducts.ProductConfigurations) as this =

    let lifetime = solution.GetSolutionLifetimes().UntilSolutionCloseLifetime
    let defaultShim = ExtensionTyping.Provider
    let outOfProcessHosting = lazy experimentalFeatures.OutOfProcessTypeProviders.Value
    let analyzeGenerativeTypeProvidersInMemory = lazy experimentalFeatures.GenerativeTypeProvidersInMemoryAnalysis.Value
    let clients = ConcurrentDictionary<TypeProvidersHostingScope, ITypeProvidersClient Lazy>()

    let getSolutionTypeProvidersClient () =
        let client = clients.GetValueSafe(TypeProvidersHostingScope.Solution)
        if isNull client then Unchecked.defaultof<_> else client.Value

    let terminateConnections () =
        for client in clients.Values do
            client.Value.Terminate()

        clients.Clear()

    let rec connect (scope: TypeProvidersHostingScope) (resolutionEnv: ResolutionEnvironment) =
        match clients.TryGetValue(scope) with
        | true, client ->
            if client.Value.IsActive then
                client.Value
            else
                clients.TryRemove(scope) |> ignore
                connect scope resolutionEnv
        | _ ->

        let client: ITypeProvidersClient Lazy =
            lazy
                let clientLifetimeDef = Lifetime.Define(lifetime)
                let isInternalMode = productConfigurations.IsInternalMode()
                let logPrefix = scope.ToString()

                let externalProcess =
                    typeProvidersProcessFactory.Create(
                        clientLifetimeDef.Lifetime,
                        Option.toObj resolutionEnv.OutputFile,
                        isInternalMode, logPrefix)

                let connection = externalProcess.Run()

                match scope with
                | Solution ->
                    SolutionTypeProvidersClient(clientLifetimeDef, connection, fcsProjectProvider, outputAssemblies,
                                                analyzeGenerativeTypeProvidersInMemory.Value) :> _
                | Scripts ->
                    ScriptTypeProvidersClient(clientLifetimeDef, connection, scriptPsiModulesProvider) :> _

        let client = clients.GetOrAdd(scope, client)
        client.Value

    do
        lifetime.Bracket((fun () -> ExtensionTyping.Provider <- this),
            fun () -> ExtensionTyping.Provider <- defaultShim)

        toolset.Changed.Advise(lifetime, fun _ -> terminateConnections ())

    interface ITypeProvidersShim with
        member this.InstantiateTypeProvidersOfAssembly(runTimeAssemblyFileName: string,
                designTimeAssemblyNameString: string, resolutionEnvironment: ResolutionEnvironment,
                isInvalidationSupported: bool, isInteractive: bool, systemRuntimeContainsType: string -> bool,
                systemRuntimeAssemblyVersion: Version, compilerToolsPath: string list,
                logError: TypeProviderError -> unit, m: range) =

            if not outOfProcessHosting.Value then
               defaultShim.InstantiateTypeProvidersOfAssembly(runTimeAssemblyFileName, designTimeAssemblyNameString,
                    resolutionEnvironment, isInvalidationSupported, isInteractive,
                    systemRuntimeContainsType, systemRuntimeAssemblyVersion, compilerToolsPath, logError, m)
            else
                let scope =
                    if isInteractive then TypeProvidersHostingScope.Scripts
                    else TypeProvidersHostingScope.Solution

                let typeProvidersClient = connect scope resolutionEnvironment
                Assertion.Assert(typeProvidersClient.IsActive, "typeProvidersClient.IsActive")
                try
                    typeProvidersClient.GetOrCreate(runTimeAssemblyFileName, designTimeAssemblyNameString,
                        resolutionEnvironment, isInvalidationSupported, isInteractive, systemRuntimeContainsType,
                        systemRuntimeAssemblyVersion, compilerToolsPath, m)
                with :? TypeProvidersInstantiationException as e  ->
                    logError (TypeProviderError(e.FcsNumber, "", m, [e.Message]))
                    []

        member this.GetProvidedTypes(pn: IProvidedNamespace) =
            match pn with
            | :? IProxyProvidedNamespace as pn -> pn.GetProvidedTypes()
            | _ -> defaultShim.GetProvidedTypes(pn)

        member this.ResolveTypeName(pn: IProvidedNamespace, typeName: string) =
            match pn with
            | :? IProxyProvidedNamespace as pn -> pn.ResolveProvidedTypeName typeName
            | _ -> defaultShim.ResolveTypeName(pn, typeName)

        member this.GetInvokerExpression(provider: ITypeProvider, method: ProvidedMethodBase, args: ProvidedVar []) =
            match provider with
            | :? IProxyTypeProvider as tp -> tp.GetInvokerExpression(method, args)
            | _ -> defaultShim.GetInvokerExpression(provider, method, args)

        member this.DisplayNameOfTypeProvider(provider: ITypeProvider, fullName: bool) =
            match provider with
            | :? IProxyTypeProvider as tp -> tp.GetDisplayName fullName
            | _ -> defaultShim.DisplayNameOfTypeProvider(provider, fullName)

        member this.RuntimeVersion() =
            let client = getSolutionTypeProvidersClient()
            if isNull client then null else client.RuntimeVersion

        member this.DumpTypeProvidersProcess() =
            clients
            |> Seq.map (fun (KeyValue(_, client)) -> client.Value.Dump())
            |> String.concat "\n\n-----------------------------------------------------\n\n"

        member this.SolutionTypeProvidersClient = getSolutionTypeProvidersClient ()

        member this.HasGenerativeTypeProviders(project) =
            let client = getSolutionTypeProvidersClient ()
            // We can determine which projects contain generative provided types
            // only from type providers hosted out-of-process
            isNotNull client &&
            client.IsActive &&
            client.As<SolutionTypeProvidersClient>().HasGenerativeTypeProviders(project)

    interface IDisposable with
        member this.Dispose() = terminateConnections ()
