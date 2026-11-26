module Module

open Expecto

let sequencedList name = testList name >> testSequenced
let wrap name = testList name

[<Tests>]
let tests =
    testList "All" [
        sequencedList "Db" [ testCase "query" <| fun _ -> () ]
        [ testCase "piped" <| fun _ -> () ] |> wrap "Piped"
    ]
