module Module

open Expecto

let makeTest name = testCase name (fun _ -> ())
let single = testCase "a" (fun _ -> ())

[<Tests>]
let first = testList "First" [ makeTest "a" ]

[<Tests>]
let second = testList "Second" [ single ]
