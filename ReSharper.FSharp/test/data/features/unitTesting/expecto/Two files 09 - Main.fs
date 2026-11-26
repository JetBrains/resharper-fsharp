module Main

open Expecto

[<Tests>]
let all =
    testList "All" [
        Db.tests
        for n in 1 .. 3 do testCase $"case {n}" ignore
    ]
