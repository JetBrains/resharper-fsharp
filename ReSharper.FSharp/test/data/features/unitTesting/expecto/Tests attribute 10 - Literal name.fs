module Module

open Expecto

[<Literal>]
let LiteralName = "literal"

let notLiteralName = "not literal"

[<Tests>]
let tests =
    testList "All" [
        testCase LiteralName <| fun _ -> ()
        testCase (LiteralName) <| fun _ -> ()
        testCase notLiteralName <| fun _ -> ()
    ]
