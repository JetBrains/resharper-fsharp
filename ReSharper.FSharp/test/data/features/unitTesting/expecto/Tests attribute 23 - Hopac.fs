module Module

open Expecto
open Hopac

[<Tests>]
let tests =
    testList "All" [
        testCaseJob "case job" <| job { return () }

        testJob "job" {
            return ()
        }
    ]
