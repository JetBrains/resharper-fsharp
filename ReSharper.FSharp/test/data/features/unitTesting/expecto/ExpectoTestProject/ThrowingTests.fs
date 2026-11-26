module ExpectoTestProject.ThrowingTests

open Expecto

[<Tests>]
let tests: Test = failwith "cannot create tests"
