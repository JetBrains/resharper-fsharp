
module TypeProviderLibrary.Caches

open FSharp.Text.RegexExtensions
open TypeProviderLibrary.SwaggerProvider
open TypeProviderLibrary.SimpleErasedProvider
open TypeProviderLibrary.SimpleGenerativeProvider
open FSharp.Management

type FsFiles = RelativePath<".", watch=true>
let regexProvider = FsFiles.``Library.fs``
let testDirectory = FsFiles.Test.Path

let client = PetStore.Client()
ignore (client.DeleteOrder(2L))

let property1: string = S.GenerativeSubtype.Property
let property2: string = S1.Property

let res1: string = RegexProvider.PhoneRegexWithPrefix().TypedMatch("425-123-2345").AreaCode.Value
let res2: string = RegexProvider.PhoneRegex().Match("425-123-2345").AreaCode.Value
let res3: string = RegexProvider.NumberRegex().TypedReplace("", fun m -> m.Number.AsInt.ToString())

let event: IEvent<Handler<string>, string> = simpleErasedType1.SimpleEvent
event.AddHandler(Handler<string>(fun s o -> ()))
event.RemoveHandler(Handler<string>(fun s o -> ()))
