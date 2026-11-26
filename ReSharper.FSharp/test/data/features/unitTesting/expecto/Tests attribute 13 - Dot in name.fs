module Module

open Expecto

[<Tests>]
let tests =
    testList "All" [
        testList "Api.V1" [
            testCase "get" <| fun _ -> ()
        ]

        testList "Api" [
            testList "V1" [
                testCase "list" <| fun _ -> ()
            ]
        ]
    ]
