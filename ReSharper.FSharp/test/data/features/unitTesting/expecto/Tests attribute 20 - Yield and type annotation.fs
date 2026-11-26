module Module

open Expecto

[<Tests>]
let yieldedTests =
    testList "Yielded" [
        yield! [ testCase "from list" ignore ]
        yield testCase "single" ignore
    ]

[<Tests>]
let typedTests =
    testList "Typed" [
        (testCase "typed" ignore : Test)
        (testCase "parenthesized" ignore)
    ]
