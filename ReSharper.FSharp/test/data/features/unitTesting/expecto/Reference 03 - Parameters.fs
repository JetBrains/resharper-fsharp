module Module

open Expecto

let testsFor (name: string) =
    testList name [
        testCase "computed child" <| fun _ -> ()
    ]

let namedTests (count: int) (flag: bool) =
    testList "Named" [
        testCase "named child" <| fun _ -> ()
    ]

[<Tests>]
let allTests =
    testList "All" [
        testsFor "Computed"
        namedTests 1 true
        (fun () -> testCase "lambda" ignore) ()
    ]
