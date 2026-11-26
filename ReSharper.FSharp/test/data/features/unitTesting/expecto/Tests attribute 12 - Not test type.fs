module Module

open Expecto

[<Tests>]
let listTests =
    [ testCase "in list" <| fun _ -> () ]

[<Tests>]
let tests =
    testList "All" [
        testCase "in test" <| fun _ -> ()
    ]
