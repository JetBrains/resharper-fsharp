module LemonadeProviderImplementation

open System.Reflection
open FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes

[<TypeProvider>]
type LemonadeProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, addDefaultProbingLocation=true)

    let ns = "ProvidedNamespace"

    do
        let myType = ProvidedTypeDefinition(Assembly.GetExecutingAssembly(), ns, "Lemonade", Some typeof<obj>)
        myType.AddMember(ProvidedMethod("Drink", [], typeof<Unit>, isStatic=true))
        this.AddNamespace(ns, [myType])

[<TypeProviderAssembly>]
do ()
