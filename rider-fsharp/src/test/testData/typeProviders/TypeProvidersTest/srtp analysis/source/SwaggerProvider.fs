module SwaggerProviderLibrary

open SwaggerProvider
open SwaggerCrossLanguage.Literals

type PetStore = OpenApiClientProvider<Schema>

let inline ctor1() = (^a : (new : unit -> ^a) ())
let inline ctor2() = (^a : (new : string -> ^a) "")
let inline ctor3() = (^a : (new : string * bool option -> ^a) ("", Some true))

let a: PetStore.CourseMateViewModel = ctor1()
let b: PetStore.CourseMateViewModel = ctor2()
let c: PetStore.CourseMateViewModel = ctor3()
