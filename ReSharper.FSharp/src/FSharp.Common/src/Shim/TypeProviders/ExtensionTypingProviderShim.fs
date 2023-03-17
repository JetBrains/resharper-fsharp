namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open FSharp.Compiler
open FSharp.Compiler.TypeProviders
open FSharp.Compiler.Text
open FSharp.Core.CompilerServices
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
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
    abstract TypeProvidersManager: IProxyTypeProvidersManager

[<SolutionComponent>]
type ExtensionTypingProviderShim(solution: ISolution, toolset: ISolutionToolset,
        experimentalFeatures: FSharpExperimentalFeaturesProvider, fcsProjectProvider: IFcsProjectProvider,
        scriptPsiModulesProvider: FSharpScriptPsiModulesProvider, outputAssemblies: OutputAssemblies,
        typeProvidersProcessFactory: TypeProvidersExternalProcessFactory,
        productConfigurations: RunsProducts.ProductConfigurations) as this =
    let lifetime = solution.GetSolutionLifetimes().UntilSolutionCloseLifetime
    let defaultShim = ExtensionTyping.Provider
    let outOfProcessHosting = lazy experimentalFeatures.OutOfProcessTypeProviders.Value
    let generativeTypeProvidersInMemoryAnalysisEnabled = lazy experimentalFeatures.GenerativeTypeProvidersInMemoryAnalysis.Value
    let createProcessLockObj = obj()

    let [<VolatileField>] mutable connection: TypeProvidersConnection = null
    let mutable typeProvidersHostLifetime: LifetimeDefinition = null
    let mutable typeProvidersManager = Unchecked.defaultof<IProxyTypeProvidersManager>

    let isConnectionAlive () =
        isNotNull connection && connection.IsActive

    let terminateConnection () =
        if isConnectionAlive() then typeProvidersHostLifetime.Terminate()

    let connect requestingProjectOutputPath =
        if isConnectionAlive () then true else

        lock createProcessLockObj (fun () ->
            if isConnectionAlive () then true else

            typeProvidersHostLifetime <- Lifetime.Define(lifetime)
            let isInternalMode = productConfigurations.IsInternalMode()
            let externalProcess =
                typeProvidersProcessFactory.Create(
                        typeProvidersHostLifetime.Lifetime,
                        requestingProjectOutputPath,
                        isInternalMode)

            if isNull externalProcess then false else
            let newConnection = externalProcess.Run()

            typeProvidersManager <- TypeProvidersManager(newConnection, fcsProjectProvider, scriptPsiModulesProvider,
                                                         outputAssemblies, generativeTypeProvidersInMemoryAnalysisEnabled.Value) :?> _
            connection <- newConnection
            true)

    do
        lifetime.Bracket((fun () -> ExtensionTyping.Provider <- this),
            fun () -> ExtensionTyping.Provider <- defaultShim)

        toolset.Changed.Advise(lifetime, fun _ -> terminateConnection ())

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
                if not (connect (Option.toObj resolutionEnvironment.OutputFile)) then [] else
                try
                    typeProvidersManager.GetOrCreate(runTimeAssemblyFileName, designTimeAssemblyNameString,
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
            if not (isConnectionAlive ()) then null else

            connection.Execute(fun _ -> connection.ProtocolModel.RdTestHost.RuntimeVersion.Sync(Unit.Instance))

        member this.DumpTypeProvidersProcess() =
            if not (isConnectionAlive ()) then raise (InvalidOperationException("Out-of-process disabled")) else

            let inProcessDump =
                $"[In-Process dump]:\n\n{typeProvidersManager.Dump()}"

            let outOfProcessDump =
                $"[Out-Process dump]:\n\n{connection.Execute(fun _ ->
                    connection.ProtocolModel.RdTestHost.Dump.Sync(Unit.Instance))}"

            $"{inProcessDump}\n\n{outOfProcessDump}"

        member this.HasGenerativeTypeProviders(project) =
            // We can determine which projects contain generative provided types
            // only from type providers hosted out-of-process
            isConnectionAlive() && typeProvidersManager.HasGenerativeTypeProviders(project)
        
        member this.TypeProvidersManager = typeProvidersManager

    interface IDisposable with
        member this.Dispose() = terminateConnection ()
