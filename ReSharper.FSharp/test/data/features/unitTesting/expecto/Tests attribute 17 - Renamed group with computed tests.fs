module Module

open Expecto

[<Tests>]
let tests =
    testList "All" [
        testList "G" [
            for i in 1 .. 2 ->
                testCase (string i) <| fun _ -> ()
        ]
    ]
