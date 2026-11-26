module Module

open Expecto

[<Tests>]
let tests =
    testList "All" [
        testCase "sync" <| fun _ ->
            ()

        test "builder" {
            ()
        }

        testCaseAsync "async" <| async { return () }

        testList "Nested" [
            ptestCase "pending" (fun _ -> ())
            ftestCase "focused" (fun _ -> ())
        ]

        testTheory "theory" [1; 2] <| fun x ->
            ()
    ]

[<FTests>]
let focused = testCase "focused root" <| fun _ -> ()

[<PTests>]
let pending () = testCase "pending root" <| fun _ -> ()
