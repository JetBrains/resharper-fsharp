module Module

open Expecto

[<Tests>]
let tests =
    testList "Generated" [
        for i in 1 .. 3 do
            testCase $"case {i}" <| fun _ ->
                ()

        testCase "static" <| fun _ -> ()
    ]
