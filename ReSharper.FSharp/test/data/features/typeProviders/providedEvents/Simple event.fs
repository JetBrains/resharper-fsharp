module Test

open SimpleErasingProviderNamespace

let event = SimpleErasedType().SimpleEvent
event.AddHandler(Handler<string>(fun s o -> ()))
event.RemoveHandler(Handler<string>(fun s o -> ()))
