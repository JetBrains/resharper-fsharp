namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open FSharp.Core.CompilerServices

type OutOfProcessProxyTypeProvider(typeProvider: ITypeProvider) =
            
    let invalidateEvent = (Event<EventHandler, EventArgs>()).Publish
                
    interface ITypeProvider with
        member this.GetNamespaces() =
                    typeProvider.GetNamespaces()
        member this.GetStaticParameters(typeWithoutArguments) =
                    typeProvider.GetStaticParameters(typeWithoutArguments)
        member this.ApplyStaticArguments(typeWithoutArguments, typePathWithArguments, staticArguments) =
                    typeProvider.ApplyStaticArguments(typeWithoutArguments, typePathWithArguments, staticArguments)
        member this.GetInvokerExpression(syntheticMethodBase, parameters) =
                    typeProvider.GetInvokerExpression(syntheticMethodBase, parameters)
        member this.GetGeneratedAssemblyContents(assembly) =
                    typeProvider.GetGeneratedAssemblyContents(assembly)
                    
        [<CLIEvent>]
        member this.Invalidate = invalidateEvent
                    
    interface IDisposable with
        member this.Dispose() = ()
