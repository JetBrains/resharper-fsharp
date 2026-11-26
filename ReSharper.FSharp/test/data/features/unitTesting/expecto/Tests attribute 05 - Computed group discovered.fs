module Module

open Expecto

[<Tests>]
let computedGroup =
    testList (string 1) [
        testCase "test" <| fun _ -> ()
    ]
