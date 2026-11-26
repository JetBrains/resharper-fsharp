module Module

open Expecto

let sharedTests =
    testList "Shared" [
        testCase "test" <| fun _ -> ()
    ]

[<Tests>]
let firstTests = testList "First" [ sharedTests ]
