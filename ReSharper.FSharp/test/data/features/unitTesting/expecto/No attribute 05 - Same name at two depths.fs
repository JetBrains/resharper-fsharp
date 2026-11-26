module Module

open Expecto

let nestedTests =
    testList "Nested" [
        testCase "same" <| fun _ -> ()
    ]

let otherTest = testCase "same" <| fun _ -> ()
