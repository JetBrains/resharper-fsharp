module Test

open SimpleErasingProviderNamespace

let event = SimpleErasedType.SimpleStaticEvent
event.AddHandler(Handler<string>(fun s o -> ()))
event.RemoveHandler(Handler<string>(fun s o -> ()))
