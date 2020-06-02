namespace ProviderImplementation

open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Quotations
open FSharp.Core.CompilerServices
open System.Reflection

type InternalType = class end

[<TypeProvider>]
type SimpleErasingProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, addDefaultProbingLocation=true)

    let ns = "SimpleErasingProviderNamespace"
    let asm = Assembly.GetExecutingAssembly()

    let createFields() =
        let stringField = ProvidedField.Literal("StringLiteralField", typeof<string>, "Const")     
        [stringField]
        
    let createProperties() =
        let stringReadonlyProperty = ProvidedProperty("ReadonlyStringProperty", typeof<string>, getterCode = fun args -> <@@ "" @@>)
        let internalTypeReadonlyProperty = ProvidedProperty("ReadonlyInternalTypeProperty", typeof<InternalType>, getterCode = fun args -> <@@ "" @@>)
        
        let stringPropertyWithSetter = ProvidedProperty(
                                            "StringPropertyWithSetter",
                                            typeof<string>,
                                            getterCode = (fun _ -> <@@ "getter" @@>),
                                            setterCode = fun _ -> <@@ "setter" @@>)
        
        let staticReadonlyProperty = ProvidedProperty(
                                        "StaticReadonlyStringProperty",
                                        typeof<string>,
                                        getterCode = (fun _ -> <@@ "" @@>),
                                        setterCode = (fun _ -> <@@ "" @@>),
                                        isStatic = true)
        
        let stringPropertyWithIndexer = ProvidedProperty(
                                            "StringPropertyWithIndexer",
                                            typeof<string>,
                                            getterCode = (fun _ -> <@@ "" @@>),
                                            indexParameters = [ProvidedParameter("Arg1", typeof<int>); ProvidedParameter("Arg2", typeof<int>)])
        
        [stringReadonlyProperty; internalTypeReadonlyProperty; stringPropertyWithSetter; staticReadonlyProperty; stringPropertyWithIndexer]
    
    let createEvents() =
        let simpleEvent = ProvidedEvent(
                            "SimpleEvent",
                            typeof<Handler<string>>,
                            adderCode = (fun _ -> Expr.Value(0)),
                            removerCode = (fun _ -> Expr.Value(0)))
        
        let simpleStaticEvent = ProvidedEvent(
                                    "SimpleStaticEvent",
                                    typeof<Handler<string>>,
                                    adderCode = (fun _ -> Expr.Value(0)),
                                    removerCode = (fun _ -> Expr.Value(0)),
                                    isStatic = true)
        
        [simpleEvent; simpleStaticEvent]
    
    let createTypes () =
        let simpleErasedType = ProvidedTypeDefinition(asm, ns, "SimpleErasedType", Some typeof<obj>)
        
        simpleErasedType.AddMembers(createFields())
        simpleErasedType.AddMembers(createProperties())
        simpleErasedType.AddMembers(createEvents())
        
        let ctor = ProvidedConstructor([], invokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        simpleErasedType.AddMember(ctor)

        let ctor2 = ProvidedConstructor([ProvidedParameter("InnerState", typeof<string>)], invokeCode = fun args -> <@@ (%%(args.[0]):string) :> obj @@>)
        simpleErasedType.AddMember(ctor2)

        let innerState = ProvidedProperty("InnerState", typeof<string>, getterCode = fun args -> <@@ (%%(args.[0]) :> obj) :?> string @@>)
        simpleErasedType.AddMember(innerState)

        //let meth = ProvidedMethod("StaticMethod", [], typeof<SomeRuntimeHelper>, isStatic=true, invokeCode = (fun args -> <@@ SomeRuntimeHelper() @@>))
        //simpleErasedType.AddMember(meth)

        //let meth2 = ProvidedMethod("StaticMethod2", [], typeof<SomeRuntimeHelper2>, isStatic=true, invokeCode = (fun args -> Expr.Value(null, typeof<SomeRuntimeHelper2>)))
        //simpleErasedType.AddMember(meth2)

        [simpleErasedType]

    do
        this.AddNamespace(ns, createTypes())

[<TypeProvider>]
type ComboGenerativeProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config)

    let ns = "ComboProvider"
    let asm = Assembly.GetExecutingAssembly()

    let createType typeName (count:int) =
        let asm = ProvidedAssembly()
        let myType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, isErased=false)

        let ctor = ProvidedConstructor([], invokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember(ctor)

        let ctor2 = ProvidedConstructor([ProvidedParameter("InnerState", typeof<string>)], invokeCode = fun args -> <@@ (%%(args.[1]):string) :> obj @@>)
        myType.AddMember(ctor2)

        for i in 1 .. count do 
            let prop = ProvidedProperty("Property" + string i, typeof<int>, getterCode = fun args -> <@@ i @@>)
            myType.AddMember(prop)

        //let meth = ProvidedMethod("StaticMethod", [], typeof<SomeRuntimeHelper>, isStatic=true, invokeCode = (fun args -> Expr.Value(null, typeof<SomeRuntimeHelper>)))
        //myType.AddMember(meth)
        //asm.AddTypes [ myType ]

        myType

    let myParamType = 
        let t = ProvidedTypeDefinition(asm, ns, "GenerativeProvider", Some typeof<obj>, isErased=false)
        t.DefineStaticParameters( [ProvidedStaticParameter("Count", typeof<int>)], fun typeName args -> createType typeName (unbox<int> args.[0]))
        t
    do
        this.AddNamespace(ns, [myParamType])


[<assembly:CompilerServices.TypeProviderAssembly()>]
do ()