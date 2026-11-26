module Module

open Expecto

[<Tests>]
let tests =
    testList "All" [
        testCase "no body"
        testCase
        testList "Partial"
        testCase "complete" <| fun _ -> ()
    ]

[<Tests>]
let openTests =
    testList "Open" [
        testCase "inner" <| fun _ -> ()
        testCase "no function" <|
