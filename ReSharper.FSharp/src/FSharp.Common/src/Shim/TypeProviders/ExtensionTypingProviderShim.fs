namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open System.Collections.Generic
open FSharp.Compiler
open FSharp.Compiler.ExtensionTyping
open FSharp.Compiler.Range
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.Rd.Tasks
open JetBrains.ReSharper.Feature.Services
open Microsoft.FSharp.Core.CompilerServices
open FSharp.Compiler.AbstractIL.Internal.Library
open FSharp.Compiler.ErrorLogger
open JetBrains.Rider.FSharp.TypeProvidersProtocol.Server
open JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter
open JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders.Hack
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models

[<SolutionComponent>]
type ExtensionTypingProviderShim (lifetime: Lifetime,
                                  onSolutionCloseNotifier: OnSolutionCloseNotifier,
                                  typeProvidersLoadersFactory: TypeProvidersLoaderExternalProcessFactory) as this =
    let defaultExtensionTypingProvider = ExtensionTypingProvider
    let mutable ourModel = null
    let outProcessLifetime = Lifetime.Define(lifetime)
    do ExtensionTypingProvider <- this :> IExtensionTypingProvider
    let onInitialized _ model =
        ourModel <- model
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(outProcessLifetime.Lifetime, fun _ -> outProcessLifetime.Terminate())
        
    let onFailed() = ()
    let typeProvidersCache: IDictionary<_, _> = Dictionary<_, _>() :> _
    
    interface IExtensionTypingProvider with
        member this.InstantiateTypeProvidersOfAssembly
                    (runTimeAssemblyFileName: string,
                     designTimeAssemblyNameString: string, 
                     resolutionEnvironment: ResolutionEnvironment, 
                     isInvalidationSupported: bool, 
                     isInteractive: bool, 
                     systemRuntimeContainsType: string -> bool, //TODO: now it always returns true
                     systemRuntimeAssemblyVersion: System.Version,
                     compilerToolsPath: string list,
                     m: range) =
            
            if ourModel == null then
                let typeProvidersLoader = typeProvidersLoadersFactory.Create(outProcessLifetime.Lifetime)
                typeProvidersLoader.RunAsync(Action<Lifetime, _>(onInitialized), Action(onFailed))
           
            let typeProviders = 
                if typeProvidersCache.ContainsKey(designTimeAssemblyNameString) then
                    typeProvidersCache.[designTimeAssemblyNameString]
                else         
                let fakeTcImports = getFakeTcImports(systemRuntimeContainsType)
                try
                    let rdTypeProviders =
                        ourModel.InstantiateTypeProvidersOfAssembly.Sync(InstantiateTypeProvidersOfAssemblyParameters(
                                                                            runTimeAssemblyFileName,
                                                                            designTimeAssemblyNameString, 
                                                                            resolutionEnvironment.toRdResolutionEnvironment(), 
                                                                            isInvalidationSupported, 
                                                                            isInteractive, 
                                                                            systemRuntimeAssemblyVersion.ToString(),
                                                                            compilerToolsPath |> Array.ofList,
                                                                            fakeTcImports), RpcTimeouts.Maximal)
                    let typeProviderProxies =
                        [for tp in rdTypeProviders -> new ProxyTypeProviderWithCache(tp, ourModel) :> ITypeProvider]
                    
                    typeProvidersCache.Add(designTimeAssemblyNameString, typeProviderProxies)
                    typeProviderProxies
                    
                with :? TypeProviderError as tpe ->
                    tpe.Iter(fun e -> errorR(NumberedError((e.Number, e.ContextualErrorMessage), m)) )                        
                    []
                    
            typeProviders
            
        member this.GetProvidedTypes(pn: IProvidedNamespace) =
            match pn with
            | :? IProxyProvidedNamespace as pn -> pn.GetProvidedTypes()
            | _ -> defaultExtensionTypingProvider.GetProvidedTypes(pn)
            
        member this.ResolveTypeName(pn: IProvidedNamespace, typeName: string) =
            match pn with
            | :? IProxyProvidedNamespace as pn -> pn.ResolveProvidedTypeName typeName
            | _ -> defaultExtensionTypingProvider.ResolveTypeName(pn, typeName)
            
        member this.GetInvokerExpression(provider: ITypeProvider, methodBase: ProvidedMethodBase, paramExprs: ProvidedVar[]) =
            match provider with
            | :? IProxyTypeProvider as tp -> tp.GetInvokerExpression(methodBase, paramExprs)
            | _ -> defaultExtensionTypingProvider.GetInvokerExpression(provider, methodBase, paramExprs)
            
        member this.DisplayNameOfTypeProvider(provider: ITypeProvider, fullName: bool) =
            match provider with
            | :? IProxyTypeProvider as tp -> tp.GetDisplayName(fullName)
            | _ -> defaultExtensionTypingProvider.DisplayNameOfTypeProvider(provider, fullName)
            
    interface IDisposable with
        member this.Dispose() = () //terminate connection
           