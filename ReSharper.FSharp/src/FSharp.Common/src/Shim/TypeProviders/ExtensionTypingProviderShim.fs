namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open FSharp.Compiler
open FSharp.Compiler.ExtensionTyping
open FSharp.Compiler.Text
open JetBrains.Core
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Exceptions
open Microsoft.FSharp.Core.CompilerServices
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models

type IProxyExtensionTypingProvider =
    abstract RuntimeVersion: unit -> string
    abstract DumpTypeProvidersProcess: unit -> string
    inherit IExtensionTypingProvider

[<SolutionComponent>]
type ExtensionTypingProviderShim(solution: ISolution, toolset: ISolutionToolset,
                                 featuresProvider: FSharpExperimentalFeaturesProvider,
                                 typeProvidersLoadersFactory: TypeProvidersLoaderExternalProcessFactory) as this =
    let solutionLifetime = solution.GetLifetime()
    let defaultExtensionTypingProvider = ExtensionTypingProvider
    let typeProvidersFeature = featuresProvider.OutOfProcessTypeProviders
    let mutable connection: TypeProvidersConnection = null
    let mutable outOfProcessLifetime: LifetimeDefinition = null
    let mutable typeProvidersManager = Unchecked.defaultof<IProxyTypeProvidersManager>

    let isConnectionAlive () = isNotNull connection && connection.IsActive
    let terminateConnection () = if isConnectionAlive() then outOfProcessLifetime.Terminate()
    let connect () =
        if not (isConnectionAlive ()) then
            outOfProcessLifetime <- Lifetime.Define(solutionLifetime)
            connection <- typeProvidersLoadersFactory.Create(outOfProcessLifetime.Lifetime).Run()
            typeProvidersManager <- TypeProvidersManager(connection) :?> _

    do solutionLifetime.Bracket((fun () -> ExtensionTypingProvider <- this),
                                fun () -> ExtensionTypingProvider <- defaultExtensionTypingProvider)
       toolset.Changed
           .Advise(solutionLifetime, fun _ -> terminateConnection ())
       typeProvidersFeature.Change
           .Advise(solutionLifetime, fun enabled -> if enabled.HasNew && not enabled.New then terminateConnection ())

    interface IProxyExtensionTypingProvider with

        member this.InstantiateTypeProvidersOfAssembly(runTimeAssemblyFileName: string,
                                                       designTimeAssemblyNameString: string,
                                                       resolutionEnvironment: ResolutionEnvironment,
                                                       isInvalidationSupported: bool,
                                                       isInteractive: bool,
                                                       systemRuntimeContainsType: string -> bool,
                                                       systemRuntimeAssemblyVersion: Version,
                                                       compilerToolsPath: string list,
                                                       logError: TypeProviderError -> unit,
                                                       m: range) =
            if not typeProvidersFeature.Value then
               defaultExtensionTypingProvider.InstantiateTypeProvidersOfAssembly(
                 runTimeAssemblyFileName, designTimeAssemblyNameString, resolutionEnvironment, isInvalidationSupported,
                 isInteractive, systemRuntimeContainsType, systemRuntimeAssemblyVersion, compilerToolsPath, logError, m)
            else
                connect()
                try
                    typeProvidersManager.GetOrCreate(
                     runTimeAssemblyFileName, designTimeAssemblyNameString, resolutionEnvironment, isInvalidationSupported,
                     isInteractive, systemRuntimeContainsType, systemRuntimeAssemblyVersion, compilerToolsPath)
                with :? TypeProvidersInstantiationException as e  ->
                    logError(TypeProviderError(e.FcsNumber, "", m, [e.Message]))
                    []

        member this.GetProvidedTypes(pn: IProvidedNamespace) =
            match pn with
            | :? IProxyProvidedNamespace as pn -> pn.GetProvidedTypes()
            | _ -> defaultExtensionTypingProvider.GetProvidedTypes(pn)

        member this.ResolveTypeName(pn: IProvidedNamespace, typeName: string) =
            match pn with
            | :? IProxyProvidedNamespace as pn -> pn.ResolveProvidedTypeName typeName
            | _ -> defaultExtensionTypingProvider.ResolveTypeName(pn, typeName)

        member this.GetInvokerExpression(provider: ITypeProvider,
                                         methodBase: ProvidedMethodBase,
                                         paramExprs: ProvidedVar []) =
            match provider with
            | :? IProxyTypeProvider as tp -> tp.GetInvokerExpression(methodBase, paramExprs)
            | _ -> defaultExtensionTypingProvider.GetInvokerExpression(provider, methodBase, paramExprs)

        member this.DisplayNameOfTypeProvider(provider: ITypeProvider, fullName: bool) =
            match provider with
            | :? IProxyTypeProvider as tp -> tp.GetDisplayName fullName
            | _ -> defaultExtensionTypingProvider.DisplayNameOfTypeProvider(provider, fullName)

        member this.RuntimeVersion() =
            if not (isConnectionAlive ()) then null else
            connection.Execute(fun _ -> connection.ProtocolModel.RdTestHost.RuntimeVersion.Sync(Unit.Instance))

        member this.DumpTypeProvidersProcess() =
            if not (isConnectionAlive ()) then raise (InvalidOperationException("Out-of-process disabled")) else

            let inProcessDump =
                $"[In-Process dump]:\n\n{typeProvidersManager.Dump()}"
            let outOfProcessDump =
                $"[Out-Process dump]:\n\n{connection.Execute(fun _ -> connection.ProtocolModel.RdTestHost.Dump.Sync(Unit.Instance))}"

            $"{inProcessDump}\n\n{outOfProcessDump}"

    interface IDisposable with
        member this.Dispose() = terminateConnection()
