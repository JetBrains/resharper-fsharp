module TypeProviderLibrary.SwaggerProvider

open SwaggerProvider

let [<Literal>] Schema = "\swagger.json"
type PetStore = OpenApiClientProvider<const (__SOURCE_DIRECTORY__ + Schema)>
let client = PetStore.Client()
ignore (client.DeleteOrder(2L))
