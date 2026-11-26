module Module

open Expecto

type SharedTests =
    static member Tests =
        testList "Shared" [
            testCase "test" <| fun _ -> ()
        ]

[<Tests>]
let allTests =
    testList "All" [
        SharedTests.Tests
    ]
