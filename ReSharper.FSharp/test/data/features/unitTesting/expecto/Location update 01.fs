module Module

open Expecto

let generatedTests =
    testList "Generated" [
        for i in 1 .. 2 ->
            testCase $"case {i}" <| fun _ -> ()
    ]
