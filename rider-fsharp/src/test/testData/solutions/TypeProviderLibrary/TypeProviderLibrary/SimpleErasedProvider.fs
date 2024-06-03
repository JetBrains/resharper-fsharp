module TypeProviderLibrary.SimpleErasedProvider

open ProviderImplementation
open SimpleErasingProviderNamespace

//Interfaces
let printable: IPrintable = ClassWithInterfaces() :> IPrintable


//Constructors
let simpleErasedType1: SimpleErasedType = SimpleErasedType()
let simpleErasedType2: SimpleErasedType = SimpleErasedType("")


//Methods
let res1: int = simpleErasedType1.MethodWithParameters(1)
let res2: int = simpleErasedType1.MethodWithParameters(1, "")
let res3: string = SimpleErasedType.StaticMethod()


//Properties
let readonlyProperty1: InternalType = simpleErasedType1.ReadonlyInternalTypeProperty
let readonlyProperty2: string = simpleErasedType1.ReadonlyStringProperty
let staticReadonlyProperty = SimpleErasedType.StaticReadonlyStringProperty
let propertyWithIndexer: string = SimpleErasedType().StringPropertyWithIndexer(2, 2)

simpleErasedType1.StringPropertyWithSetter <- ""


//Fields
let field: string = SimpleErasedType.StringLiteralField


//Events
let event: IEvent<Handler<string>, string> = simpleErasedType1.SimpleEvent
event.AddHandler(Handler<string>(fun s o -> ()))
event.RemoveHandler(Handler<string>(fun s o -> ()))

let staticEvent: IEvent<Handler<string>, string> = SimpleErasedType.SimpleStaticEvent
staticEvent.AddHandler(Handler<string>(fun s o -> ()))
staticEvent.RemoveHandler(Handler<string>(fun s o -> ()))
