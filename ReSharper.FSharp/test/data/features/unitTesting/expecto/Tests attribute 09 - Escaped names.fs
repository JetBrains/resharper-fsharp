module Module

open Expecto

[<Tests>]
let tests =
    testList "All" [
        testCase "tab\tname" <| fun _ -> ()
        testCase @"verbatim ""quoted"" \name" <| fun _ -> ()
        testCase """triple "quoted" \name""" <| fun _ -> ()
        testCase "quote \" name" <| fun _ -> ()
    ]
