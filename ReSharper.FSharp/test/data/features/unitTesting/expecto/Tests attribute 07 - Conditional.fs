module Module

open Expecto

[<Tests>]
let tests =
    testList "All" [
        testCase "always" <| fun _ -> ()

        if System.Environment.Is64BitProcess then
            testCase "conditional" <| fun _ -> ()
        else
            testCase "other" <| fun _ -> ()

        match System.Environment.ProcessorCount with
        | 1 -> testCase "single" <| fun _ -> ()
        | _ -> testCase "many" <| fun _ -> ()
    ]
