module Module

open Expecto

[<Tests>]
let tests =
    testList "All" (
        [ testCase "first" <| fun _ -> ()
          testCase "second" <| fun _ -> () ].Tail)
