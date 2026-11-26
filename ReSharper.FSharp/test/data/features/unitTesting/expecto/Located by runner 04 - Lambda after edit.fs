module Module

open Expecto

let withValue (createTest: int -> Test) = createTest 1

[<Tests>]
let tests =
    testList "All" [
        withValue (fun _ -> testCase "first" <| fun _ -> ())
        withValue (fun _ -> testCase "second" <| fun _ -> ())
    ]
