module TypeProviderLibrary.SwaggerProvider

open SwaggerProvider

let [<Literal>] Schema = "https://petstore.swagger.io/v2/swagger.json"
type PetStore = OpenApiClientProvider<Schema>
let client = PetStore.Client()
ignore (client.DeleteOrder(2L))
