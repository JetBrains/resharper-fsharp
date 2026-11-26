module Module

open Expecto

[<Tests>]
let tests =
    testList "All" [
        testTheory "theory" [1; 2] <| fun x ->
            Expect.isGreaterThan x 0 "positive"
    ]
