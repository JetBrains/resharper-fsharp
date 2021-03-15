module TypeProviderLibrary.ProvidersErrors

open SwaggerProvider
open FSharp.Configuration
open FSharp.Text.RegexProvider
open SimpleErasingProviderNamespace

let [<Literal>] Schema = "https://petstore.swagger.io/v2/swagger.json"

type PetStore1 = OpenApiClientProvider<Schema>
type PetStore2 = OpenApiClientProvider<Schema>


let _  = YamlConfig<"1.txt">

let abstractInstance = AbstractType()
SimpleErasedType().ReadonlyStringProperty <- ""

type NumberRegex = Regex< 1 >
