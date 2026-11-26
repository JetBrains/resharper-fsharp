module Module

open Expecto

let wrap (test: Test) = testList "Wrapper" [ test ]

[<Tests>]
let tests =
    testList "All" [
        wrap (testCase "direct" <| fun _ -> ())
        testCase "piped" ignore |> wrap
        Some(testCase "in option" ignore) |> Option.get
        [ testCase "to lambda" ignore ] |> fun tests -> testList "Lambda" tests
    ]
