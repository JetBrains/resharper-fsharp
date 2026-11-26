module Main

open Expecto

[<Tests>]
let tests =
    testList "All" [
        Lists.parserTests
        testCase "direct" <| fun _ -> ()
    ]
