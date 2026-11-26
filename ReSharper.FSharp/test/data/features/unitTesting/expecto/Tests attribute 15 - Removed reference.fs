module Module

open Expecto

let parserTests =
    testList "Parser" [
        testCase "numbers" <| fun _ -> ()
    ]

[<Tests>]
let tests =
    testList "All" [
        testCase "direct" <| fun _ -> ()
        parserTests
    ]
