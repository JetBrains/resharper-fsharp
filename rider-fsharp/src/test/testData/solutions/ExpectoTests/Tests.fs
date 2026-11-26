module Tests

open Expecto

[<Tests>]
let tests =
    testList "All" [
        testCase "literal" <| fun _ -> ()

        for n in 1 .. 2 ->
            testCase $"case {n}" <| fun _ -> ()
    ]
