module Main

open Expecto

let shared =
    testList "Local" [
        testCase "local" <| fun _ -> ()
    ]

[<Tests>]
let tests = testList "All" [ Lists.shared ]
