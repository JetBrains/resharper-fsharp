module Module

open Expecto

[<Literal>]
let Prefix = "prefix "

[<Tests>]
let tests =
    testList ("Concat" + "enated") [
        testCase ("first" + " " + "test") <| fun _ -> ()
        testCase (Prefix + "test") <| fun _ -> ()
    ]
