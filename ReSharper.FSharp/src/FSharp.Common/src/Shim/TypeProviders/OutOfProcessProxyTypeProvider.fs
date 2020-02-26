namespace JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders

open System
open FSharp.Core.CompilerServices
open JetBrains.Rider.FSharp.TypeProvidersProtocol.Server
open JetBrains.ReSharper.Plugins.FSharp.Util.TypeProvidersProtocolConverter

type OutOfProcessProxyTypeProvider(typeProvider: RdTypeProvider) =
            
    let invalidateEvent = (Event<EventHandler, EventArgs>()).Publish
                
    interface ITypeProvider with
        member this.GetNamespaces() =
            typeProvider.GetNamespaces.Sync(JetBrains.Core.Unit.Instance) |> Array.map (fun x -> x.toProvidedNamespace())

        member this.GetStaticParameters(typeWithoutArguments) =
                    [||]
                    
        member this.ApplyStaticArguments(typeWithoutArguments, typePathWithArguments, staticArguments) =
                    //typeProvider.ApplyStaticArguments(typeWithoutArguments, typePathWithArguments, staticArguments)
                    null
        member this.GetInvokerExpression(syntheticMethodBase, parameters) =
                    //typeProvider.GetInvokerExpression(syntheticMethodBase, parameters)
                    raise null
                    
        member this.GetGeneratedAssemblyContents(assembly) =
                    //typeProvider.GetGeneratedAssemblyContents(assembly)
                    null
                    
        [<CLIEvent>]
        member this.Invalidate = invalidateEvent
                    
    interface IDisposable with
        member this.Dispose() = () //remote dispose
