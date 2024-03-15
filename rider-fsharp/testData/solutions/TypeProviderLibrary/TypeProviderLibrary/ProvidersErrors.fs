module TypeProviderLibrary.ProvidersErrors

open SwaggerProvider
open FSharp.Configuration
open FSharp.Text.RegexProvider
open SimpleErasingProviderNamespace

let [<Literal>] Schema = "\swagger.json"

type PetStore1 = OpenApiClientProvider<const (__SOURCE_DIRECTORY__ + Schema)>
type PetStore2 = OpenApiClientProvider<const (__SOURCE_DIRECTORY__ + Schema)>


let _  = YamlConfig<"1.txt">

let abstractInstance = AbstractType()
SimpleErasedType().ReadonlyStringProperty <- ""

type NumberRegex = Regex< 1 >

let f (x: PetStore.Pet): string = x.Category.Name
let g (x: PetStore.OperationTypes.UpdatePetWithForm_formUrlEncoded): string = x.Name
