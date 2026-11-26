module Module

open Expecto

let testsFor (name: string) =
    testList name [
        testCase "test" <| fun _ -> ()
    ]

[<Tests>]
let firstTests =
    testList "First" [
        testsFor "Computed"
    ]

[<Tests>]
let secondTests =
    testList "Second" [
        testList "Named" [
            testCase "test" <| fun _ -> ()
        ]

        testList (string 1) [
            testCase "test" <| fun _ -> ()
        ]
    ]
