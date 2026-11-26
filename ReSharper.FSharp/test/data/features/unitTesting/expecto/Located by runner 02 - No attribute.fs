module Module

open Expecto

let parserTests =
    testList "Parser" [
        for i in 1 .. 2 do
            testCase (string i) <| fun _ -> ()

        testCase "static" <| fun _ -> ()
    ]
