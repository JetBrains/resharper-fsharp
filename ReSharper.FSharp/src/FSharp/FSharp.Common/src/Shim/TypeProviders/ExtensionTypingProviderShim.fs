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
open JetBrains.Core
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Build
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions
open JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models

type IProxyExtensionTypingProvider =
    inherit IExtensionTypingProvider

    abstract RuntimeVersion: unit -> string
    abstract HasGenerativeTypeProviders: project: IProject -> bool
    abstract DumpTypeProvidersProcess: unit -> string
    abstract TypeProvidersManager: ITypeProvidersManager


[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type ExtensionTypingProviderShim(solution: ISolution, toolset: ISolutionToolset,
        experimentalFeatures: FSharpExperimentalFeaturesProvider, fcsProjectProvider: IFcsProjectProvider,
        scriptPsiModulesProvider: FSharpScriptPsiModulesProvider, outputAssemblies: OutputAssemblies,
        typeProvidersProcessFactory: TypeProvidersExternalProcessFactory,
        productConfigurations: RunsProducts.ProductConfigurations) as this =
    let lifetime = solution.GetSolutionLifetimes().UntilSolutionCloseLifetime
    let defaultShim = ExtensionTyping.Provider
    let outOfProcessHosting = lazy experimentalFeatures.OutOfProcessTypeProviders.Value
    let generativeTypeProvidersInMemoryAnalysisEnabled = lazy experimentalFeatures.GenerativeTypeProvidersInMemoryAnalysis.Value

    let dict = ConcurrentDictionary<TypeProvidersHostScope, ITypeProvidersManager>()
    let getSolutionTypeProviders () = dict.GetValueSafe(TypeProvidersHostScope.Solution)

    let terminateConnections () =
        for connection in dict.Values do
            connection.Terminate()

    let connect (scope: TypeProvidersHostScope) (resolutionEnv: ResolutionEnvironment) =
        match dict.TryGetValue(scope) with
        | true, manager when manager.Connection.IsActive -> Some manager
        | _ ->

        let typeProvidersHostLifetime = Lifetime.Define(lifetime)
        let isInternalMode = productConfigurations.IsInternalMode()
        let externalProcess =
            typeProvidersProcessFactory.Create(
                    typeProvidersHostLifetime.Lifetime,
                    Option.toObj resolutionEnv.OutputFile,
                    isInternalMode)

        if isNull externalProcess then None else
        let newConnection = externalProcess.Run()

        let tpManager: ITypeProvidersManager =
            match scope with
            | Solution ->
                SolutionTypeProvidersManager(typeProvidersHostLifetime, newConnection, fcsProjectProvider, outputAssemblies,
                                             generativeTypeProvidersInMemoryAnalysisEnabled.Value)
            | Script _ ->
                ScriptTypeProvidersManager(typeProvidersHostLifetime, newConnection, scriptPsiModulesProvider)

        dict.TryAdd(scope, tpManager) |> ignore
        Some tpManager

    do
        lifetime.Bracket((fun () -> ExtensionTyping.Provider <- this),
            fun () -> ExtensionTyping.Provider <- defaultShim)

        toolset.Changed.Advise(lifetime, fun _ -> terminateConnections ())

    interface IProxyExtensionTypingProvider with
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
                    if isInteractive then TypeProvidersHostScope.Script(m.FileName)
                    else TypeProvidersHostScope.Solution

                let typeProvidersManager = connect scope resolutionEnvironment
                if isNull typeProvidersManager then [] else
                try
                    typeProvidersManager.Value.GetOrCreate(runTimeAssemblyFileName, designTimeAssemblyNameString,
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
            let typeProvidersManager = getSolutionTypeProviders()
            if isNull typeProvidersManager then null else

            let connection = typeProvidersManager.Connection
            if not connection.IsActive then null else

            connection.Execute(fun _ -> connection.ProtocolModel.RdTestHost.RuntimeVersion.Sync(Unit.Instance))

        member this.DumpTypeProvidersProcess() =
            let typeProvidersManager = getSolutionTypeProviders()
            if isNull typeProvidersManager then null else
            if isNull typeProvidersManager || not typeProvidersManager.Connection.IsActive then raise (InvalidOperationException("Out-of-process disabled")) else

            let inProcessDump =
                $"[In-Process dump]:\n\n{typeProvidersManager.Dump()}"

            let outOfProcessDump =
                $"[Out-Process dump]:\n\n{typeProvidersManager.Connection.Execute(fun _ ->
                    typeProvidersManager.Connection.ProtocolModel.RdTestHost.Dump.Sync(Unit.Instance))}"

            $"{inProcessDump}\n\n{outOfProcessDump}"

        member this.HasGenerativeTypeProviders(project) =
            // We can determine which projects contain generative provided types
            // only from type providers hosted out-of-process
            match dict.TryGetValue(TypeProvidersHostScope.Solution) with
            | true, (:? SolutionTypeProvidersManager as solutionManager) ->
                solutionManager.HasGenerativeTypeProviders(project)

            | _ -> false

        member this.TypeProvidersManager = dict.GetValueSafe(TypeProvidersHostScope.Solution)

    interface IDisposable with
        member this.Dispose() = terminateConnections ()
