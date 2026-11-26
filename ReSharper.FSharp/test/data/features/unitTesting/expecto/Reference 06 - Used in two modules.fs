module Module

open Expecto

let shared =
    testList "Shared" [
        testCase "x" <| fun _ -> ()
    ]

module First =
    [<Tests>]
    let tests = testList "First" [ shared ]

module Second =
    [<Tests>]
    let tests = testList "Second" [ shared ]
