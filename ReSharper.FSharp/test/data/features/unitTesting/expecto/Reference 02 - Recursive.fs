module Module

open Expecto

let rec recursiveTests () =
    testList "Recursive" [
        testCase "test" <| fun _ -> ()
        recursiveTests ()
    ]

[<Tests>]
let allTests =
    testList "All" [
        recursiveTests ()
    ]
