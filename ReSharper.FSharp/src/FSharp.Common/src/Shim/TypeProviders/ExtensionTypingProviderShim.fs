namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open System.Collections.Generic
open FSharp.Compiler
open System.IO
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

module FCSTypeProviderErrors =
    let etTypeProviderConstructorException (exnMessage: string) =
        (3053, sprintf "The type provider constructor has thrown an exception: %s" exnMessage)      
        
    let etProviderDoesNotHaveValidConstructor =
        (3032, "The type provider does not have a valid constructor. A constructor taking either no arguments or one argument of type 'TypeProviderConfig' was expected.")
       
    let etInvalidTypeProviderAssemblyName (runTimeAssemblyFileName, designTimeAssemblyNameString) =
        (3065, sprintf "Assembly '%s' has TypeProviderAssembly attribute with invalid value '%s'. The value should be a valid assembly name" runTimeAssemblyFileName designTimeAssemblyNameString)
        
    let etProviderHasWrongDesignerAssembly(tpAsmAtrName, designTimeAssemblyNameString, errorMessage) =
        (3031, sprintf "Assembly attribute '%s' refers to a designer assembly '%s' which cannot be loaded or doesn't exist. %s" tpAsmAtrName designTimeAssemblyNameString errorMessage)

module TypeProviderInstantiateHelpers =
        //range можем не сериализовать
        let GetTypeProviderImplementationTypes (runTimeAssemblyFileName, designTimeAssemblyNameString: string) =

        // Report an error, blaming the particular type provider component
        let raiseError (e: exn) =
            //raise (TypeProviderError(FCSTypeProviderErrors.etProviderHasWrongDesignerAssembly(typeof<TypeProviderAssemblyAttribute>.Name, designTimeAssemblyNameString, e.Message), runTimeAssemblyFileName, null))
            failwith "лох"
        // Find and load the designer assembly for the type provider component.
        //
        // We look in the directories stepping up from the location of the runtime assembly.

        let loadFromLocation designTimeAssemblyPath =
            try
                Some (FileSystem.AssemblyLoadFrom designTimeAssemblyPath)
            with e ->
                raiseError e

        let rec searchParentDirChain dir designTimeAssemblyName = 
            seq { 
                for subdir in toolingCompatiblePaths() do
                    let designTimeAssemblyPath  = Path.Combine (dir, subdir, designTimeAssemblyName)
                    if FileSystem.SafeExists designTimeAssemblyPath then 
                        yield loadFromLocation designTimeAssemblyPath
                match Path.GetDirectoryName dir with
                | s when String.IsNullOrEmpty(s) || Path.GetFileName dir = "packages" || s = dir -> ()
                | parentDir -> yield! searchParentDirChain parentDir designTimeAssemblyName 
            } 

        let loadFromParentDirRelativeToRuntimeAssemblyLocation designTimeAssemblyName = 
            let runTimeAssemblyPath = Path.GetDirectoryName runTimeAssemblyFileName
            searchParentDirChain runTimeAssemblyPath designTimeAssemblyName
            |> Seq.tryHead
            |> function 
               | Some res -> res 
               | None -> 
                // The search failed, just load from the first location and report an error
                let runTimeAssemblyPath = Path.GetDirectoryName runTimeAssemblyFileName
                loadFromLocation (Path.Combine (runTimeAssemblyPath, designTimeAssemblyName))

        let designTimeAssemblyOpt = 

            if designTimeAssemblyNameString.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) then
                loadFromParentDirRelativeToRuntimeAssemblyLocation designTimeAssemblyNameString
            else
                // Cover the case where the ".dll" extension has been left off and no version etc. has been used in the assembly
                // string specification.  The Name=FullName comparison is particularly strange, and was there to support
                // design-time DLLs specified using "x.DesignTIme, Version= ..." long assembly names and GAC loads.
                // These kind of design-time assembly specifications are no longer used to our knowledge so that comparison is basically legacy
                // and will always succeed.  
                let name = System.Reflection.AssemblyName (Path.GetFileNameWithoutExtension designTimeAssemblyNameString)
                if name.Name.Equals(name.FullName, StringComparison.OrdinalIgnoreCase) then
                    let designTimeAssemblyName = designTimeAssemblyNameString + ".dll"
                    loadFromParentDirRelativeToRuntimeAssemblyLocation designTimeAssemblyName
                else
                    // Load from the GAC using Assembly.Load.  This is legacy since type provider design-time components are
                    // never in the GAC these days and  "x.DesignTIme, Version= ..." specifications are never used.
                    try
                        let asmName = System.Reflection.AssemblyName designTimeAssemblyNameString
                        Some (FileSystem.AssemblyLoad asmName)
                    with e ->
                        raiseError e

        match designTimeAssemblyOpt with
        | Some loadedDesignTimeAssembly ->
            try
                let exportedTypes = loadedDesignTimeAssembly.GetExportedTypes()
                let filtered = [ for t in exportedTypes do 
                                 let ca = t.GetCustomAttributes(true) |> Seq.filter (fun x -> x.GetType().Name = "TypeProviderAttribute") |> Seq.toArray
                                 if ca <> null && ca.Length > 0 then yield t ]
                filtered
            with e ->
                raiseError e
        | None -> []

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
                     systemRuntimeContainsType: string -> bool, //не забыть
                     systemRuntimeAssemblyVersion: System.Version,
                     compilerToolsPath: string list, //не забыть
                     m: range) =
            
            if ourModel = null then 
                let typeProvidersLoader = typeProvidersLoadersFactory.Create(lifetime)
                typeProvidersLoader.RunAsync(Action<_, _>(onInitialized), Action(onFailed))
           
            let typeProviders = 
                if typeProvidersCache.ContainsKey(designTimeAssemblyNameString) then
                    typeProvidersCache.[designTimeAssemblyNameString]
                else 
            
                let fakeTcImports = getFakeTcImports(systemRuntimeContainsType)
            
            //TODO: need to secure lifetime 
                let rdSystemRuntimeContainsType = RdSystemRuntimeContainsType(SystemRuntimeContainsTypeRef(Value(fakeTcImports)))
                //rdSystemRuntimeContainsType.SystemRuntimeContainsTypeRef.Value.ConSystemRuntimeContainsType.Set(fun a b -> RdTask.Successful(systemRuntimeContainsType b))
            
                try
                    let designTimeAssemblyName = 
                        try
                            if designTimeAssemblyNameString.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) then
                                Some (System.Reflection.AssemblyName (Path.GetFileNameWithoutExtension designTimeAssemblyNameString))
                            else
                                Some (System.Reflection.AssemblyName designTimeAssemblyNameString)
                        with :? ArgumentException ->
                            errorR(Error(FCSTypeProviderErrors.etInvalidTypeProviderAssemblyName(runTimeAssemblyFileName, designTimeAssemblyNameString), m))
                            None

                    let newTypeProviders = 
                        [ match designTimeAssemblyName, resolutionEnvironment.outputFile with
                            | Some designTimeAssemblyName, Some path when String.Compare(designTimeAssemblyName.Name,
                                                                                         Path.GetFileNameWithoutExtension path,
                                                                                         StringComparison.OrdinalIgnoreCase) = 0 -> ()
                            | Some _, _ ->
                            let res = ourModel.InstantiateTypeProvidersOfAssembly.Sync(InstantiateTypeProvidersOfAssemblyParameters(
                                                                                                                                  runTimeAssemblyFileName,
                                                                                                                                  designTimeAssemblyNameString, 
                                                                                                                                  resolutionEnvironment.toRdResolutionEnvironment(), 
                                                                                                                                  isInvalidationSupported, 
                                                                                                                                  isInteractive, 
                                                                                                                                  systemRuntimeAssemblyVersion.toRdVersion(),
                                                                                                                                  compilerToolsPath |> Array.ofList,
                                                                                                                                  rdSystemRuntimeContainsType))
                            for tp in res
                                -> new ProxyTypeProviderWithCache(tp, ourModel) :> ITypeProvider
                            |   None, _ -> () ]
                    
                    typeProvidersCache.Add(designTimeAssemblyNameString, newTypeProviders)
                    newTypeProviders

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
           