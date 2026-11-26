module Module

open Expecto

let parserTests =
    testList "Parser" [
        testCase "numbers" <| fun _ -> ()
    ]

let lexerTests () =
    testList "Lexer" [
        testCase "tokens" <| fun _ -> ()
    ]

[<Tests>]
let allTests =
    testList "All" [
        parserTests
        lexerTests ()
        testCase "direct" <| fun _ -> ()
    ]
