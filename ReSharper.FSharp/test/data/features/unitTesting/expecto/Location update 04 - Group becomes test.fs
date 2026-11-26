module Module

open Expecto

[<Tests>]
let tests =
    testList "All" [
        testList "x" [
            for i in 1 .. 2 ->
                testCase (string i) <| fun _ -> ()
        ]
    ]
