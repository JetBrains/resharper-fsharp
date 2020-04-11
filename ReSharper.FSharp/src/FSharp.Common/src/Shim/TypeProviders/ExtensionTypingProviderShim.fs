namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open System.Collections.Generic
open FSharp.Compiler
open System.IO
open System.Threading
open FSharp.Compiler.AbstractIL.IL
open FSharp.Compiler.ExtensionTyping
open FSharp.Compiler.Range
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open Microsoft.FSharp.Core.CompilerServices
open FSharp.Compiler.AbstractIL.Internal.Library
open FSharp.Compiler.ErrorLogger
open JetBrains.Rider.FSharp.TypeProvidersProtocol.Server
open JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter
open JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders.Hack
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models

[<SolutionComponent>]
type ExtensionTypingProviderShim (lifetime: Lifetime,
                                  typeProvidersLoadersFactory: TypeProvidersLoaderExternalProcessFactory) as this =
    let defaultExtensionTypingProvider = ExtensionTypingProvider
    let mutable ourModel = null
    
    do ExtensionTypingProvider <- this :> IExtensionTypingProvider
       lifetime.OnTermination(fun () ->
           (this :> IDisposable).Dispose()
           ExtensionTypingProvider <- defaultExtensionTypingProvider) |> ignore
       ()
       
    let onInitialized lifetime model =
        ourModel <- model
        ()
        
    let onFailed() = ()
    let typeProvidersCache: IDictionary<string, ITypeProvider list> = Dictionary<string, ITypeProvider list>() :> _
    
    interface IExtensionTypingProvider with
        member this.InstantiateTypeProvidersOfAssembly
                    (runTimeAssemblyFileName: string, 
                     ilScopeRefOfRuntimeAssembly: ILScopeRef, 
                     designTimeAssemblyNameString: string, 
                     resolutionEnvironment: ResolutionEnvironment, 
                     isInvalidationSupported: bool, 
                     isInteractive: bool, 
                     systemRuntimeContainsType: string -> bool, //TODO: не забыть
                     systemRuntimeAssemblyVersion: System.Version,
                     compilerToolsPath: string list, //TODO: не забыть
                     m: range) =
            
            if ourModel = null then 
                let typeProvidersLoader = typeProvidersLoadersFactory.Create(lifetime)
                typeProvidersLoader.RunAsync(Action<_, _>(onInitialized), Action(onFailed))
                Thread.Sleep(5000)
           
            let typeProviders = 
                if typeProvidersCache.ContainsKey(designTimeAssemblyNameString) then
                    typeProvidersCache.[designTimeAssemblyNameString]
                else 
            
                let fakeTcImports = getFakeTcImports(systemRuntimeContainsType)
            
            //TODO: need to secure lifetime 
                let rdSystemRuntimeContainsType = RdSystemRuntimeContainsType(SystemRuntimeContainsTypeRef(Value(fakeTcImports)))
                //rdSystemRuntimeContainsType.SystemRuntimeContainsTypeRef.Value.ConSystemRuntimeContainsType.Set(fun a b -> RdTask.Successful(systemRuntimeContainsType b))
            
                try
                    let rdTypeProviders =
                        ourModel.InstantiateTypeProvidersOfAssembly.Sync(InstantiateTypeProvidersOfAssemblyParameters(
                                                                            runTimeAssemblyFileName,
                                                                            ilScopeRefOfRuntimeAssembly.toRdILScopeRef(),
                                                                            designTimeAssemblyNameString, 
                                                                            resolutionEnvironment.toRdResolutionEnvironment(), 
                                                                            isInvalidationSupported, 
                                                                            isInteractive, 
                                                                            systemRuntimeAssemblyVersion.ToString(),
                                                                            compilerToolsPath |> Array.ofList,
                                                                            rdSystemRuntimeContainsType))
                    let typeProviderProxies =
                        [for tp in rdTypeProviders -> new ProxyTypeProviderWithCache(tp, ourModel) :> ITypeProvider]
                    
                    typeProvidersCache.Add(designTimeAssemblyNameString, typeProviderProxies)
                    typeProviderProxies
                    
                with :? TypeProviderError as tpe ->
                    tpe.Iter(fun e -> errorR(NumberedError((e.Number, e.ContextualErrorMessage), m)) )                        
                    [] //local try catch
                    
            let providerSpecs = typeProviders |> List.map (fun t -> (t, ilScopeRefOfRuntimeAssembly))
            let taintedProviders = Tainted<_>.CreateAll providerSpecs
            taintedProviders
            
        member this.GetProvidedTypes(pn: Tainted<IProvidedNamespace>, m: range) =
            let providedTypes =
                pn.PApplyArray((fun r -> r.As<IProxyProvidedNamespace>().GetProvidedTypes()), "GetTypes", m)
            providedTypes
            
        member this.ResolveTypeName(pn: Tainted<IProvidedNamespace>, typeName: string, m: range) =
            pn.PApply((fun providedNamespace ->
                (providedNamespace.As<IProxyProvidedNamespace>().ResolveProvidedTypeName typeName)), range=m) 
            
    interface IDisposable with
        member this.Dispose() = () //terminate connection
           