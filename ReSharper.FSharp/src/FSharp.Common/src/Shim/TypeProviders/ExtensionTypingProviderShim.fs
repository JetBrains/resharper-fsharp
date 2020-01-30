namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
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

module FCSTypeProviderErrors =
    let etTypeProviderConstructorException (exnMessage: string) =
        (3053, sprintf "The type provider constructor has thrown an exception: %s" exnMessage)      
        
    let etProviderDoesNotHaveValidConstructor =
        (3032, "The type provider does not have a valid constructor. A constructor taking either no arguments or one argument of type 'TypeProviderConfig' was expected.")
       
    let etInvalidTypeProviderAssemblyName (runTimeAssemblyFileName, designTimeAssemblyNameString) =
        (3065, sprintf "Assembly '%s' has TypeProviderAssembly attribute with invalid value '%s'. The value should be a valid assembly name" runTimeAssemblyFileName designTimeAssemblyNameString)
        
    let etProviderHasWrongDesignerAssembly(tpAsmAtrName, designTimeAssemblyNameString, errorMessage) =
        (3031, sprintf "Assembly attribute '%s' refers to a designer assembly '%s' which cannot be loaded or doesn't exist. %s" tpAsmAtrName designTimeAssemblyNameString errorMessage)

[<SolutionComponent>]
type ExtensionTypingProviderShim (lifetime: Lifetime) as this =
 
    let StripException (e: exn) =
        match e with
        |   :? System.Reflection.TargetInvocationException as e -> e.InnerException
        |   :? TypeInitializationException as e -> e.InnerException
        |   _ -> e
   
    let defaultExtensionTypingProvider = ExtensionTypingProvider
    
    let CreateTypeProvider (typeProviderImplementationType: System.Type, 
                            runtimeAssemblyPath, 
                            resolutionEnvironment: ResolutionEnvironment, 
                            isInvalidationSupported: bool, 
                            isInteractive: bool, 
                            systemRuntimeContainsType: string -> bool, 
                            systemRuntimeAssemblyVersion, 
                            m) =

        // Protect a .NET reflection call as we load the type provider component into the host process, 
        // reporting errors.
        let protect f =
            try 
                f ()
            with err ->
                let e = StripException (StripException err)
                raise (TypeProviderError(FCSTypeProviderErrors.etTypeProviderConstructorException(e.Message), typeProviderImplementationType.FullName, m))

        if typeProviderImplementationType.GetConstructor([| typeof<TypeProviderConfig> |]) <> null then

            let tpConfig = TypeProviderConfig(systemRuntimeContainsType, 
                                              ResolutionFolder=resolutionEnvironment.resolutionFolder, 
                                              RuntimeAssembly=runtimeAssemblyPath, 
                                              ReferencedAssemblies=Array.copy resolutionEnvironment.referencedAssemblies, 
                                              TemporaryFolder=resolutionEnvironment.temporaryFolder, 
                                              IsInvalidationSupported=isInvalidationSupported, 
                                              IsHostedExecution= isInteractive, 
                                              SystemRuntimeAssemblyVersion = systemRuntimeAssemblyVersion)

            protect (fun () -> Activator.CreateInstance(typeProviderImplementationType, [| box tpConfig|]) :?> ITypeProvider )

        elif typeProviderImplementationType.GetConstructor [| |] <> null then 
            protect (fun () -> Activator.CreateInstance typeProviderImplementationType :?> ITypeProvider )

        else
            raise (TypeProviderError(FCSTypeProviderErrors.etProviderDoesNotHaveValidConstructor, typeProviderImplementationType.FullName, m))
      
    let GetTypeProviderImplementationTypes (runTimeAssemblyFileName, designTimeAssemblyNameString, m: range) =

        // Report an error, blaming the particular type provider component
        let raiseError (e: exn) =
            raise (TypeProviderError(FCSTypeProviderErrors.etProviderHasWrongDesignerAssembly(typeof<TypeProviderAssemblyAttribute>.Name, designTimeAssemblyNameString, e.Message), runTimeAssemblyFileName, m))

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
                                 let ca = t.GetCustomAttributes(typeof<TypeProviderAttribute>, true)
                                 if ca <> null && ca.Length > 0 then yield t ]
                filtered
            with e ->
                raiseError e
        | None -> []
    
    do ExtensionTypingProvider <- this :> IExtensionTypingProvider
       lifetime.OnTermination(fun () -> ExtensionTypingProvider <- defaultExtensionTypingProvider) |> ignore
       ()
    
    interface IExtensionTypingProvider with
        member this.InstantiateTypeProvidersOfAssembly
                    (runTimeAssemblyFileName: string, 
                     ilScopeRefOfRuntimeAssembly: ILScopeRef, 
                     designTimeAssemblyNameString: string, 
                     resolutionEnvironment: ResolutionEnvironment, 
                     isInvalidationSupported: bool, 
                     isInteractive: bool, 
                     systemRuntimeContainsType: string -> bool, 
                     systemRuntimeAssemblyVersion: System.Version,
                     m: range) =
            
            let providerSpecs = 
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

                    [ match designTimeAssemblyName, resolutionEnvironment.outputFile with
                      | Some designTimeAssemblyName, Some path when String.Compare(designTimeAssemblyName.Name,
                                                                                   Path.GetFileNameWithoutExtension path,
                                                                                   StringComparison.OrdinalIgnoreCase) = 0 -> ()
                      | Some _, _ ->
                          for tp in GetTypeProviderImplementationTypes (runTimeAssemblyFileName, designTimeAssemblyNameString, m) do
                            let tpInstance = CreateTypeProvider (tp, runTimeAssemblyFileName, resolutionEnvironment,
                                                                 isInvalidationSupported, isInteractive,
                                                                 systemRuntimeContainsType,
                                                                 systemRuntimeAssemblyVersion,
                                                                 m)
                            if box tpInstance <> null then yield (tpInstance, ilScopeRefOfRuntimeAssembly)
                      |   None, _ -> () ]

                with :? TypeProviderError as tpe ->
                    tpe.Iter(fun e -> errorR(NumberedError((e.Number, e.ContextualErrorMessage), m)) )                        
                    []

            let providers = Tainted<_>.CreateAll providerSpecs
            providers
           