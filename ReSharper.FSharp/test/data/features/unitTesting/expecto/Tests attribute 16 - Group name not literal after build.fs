module Module

open Expecto

let groupName = "Comp"

[<Tests>]
let tests =
    testList "All" [
        testList "Comp" [
            testCase "x" <| fun _ -> ()
        ]
    ]
