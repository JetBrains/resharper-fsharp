module Module

open Expecto

let parserTests =
    testList "Parser" [
        testCase "numbers" <| fun _ -> ()
        testCase "strings" <| fun _ -> ()
        testCase "not discovered" <| fun _ -> ()
    ]
