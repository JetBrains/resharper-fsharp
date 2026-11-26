module Module

open Expecto

let testCase (name: string) (f: unit -> unit) = Expecto.Tests.testCase name f
let testList (name: string) (tests: Test list) = Expecto.Tests.testList name tests

[<Tests>]
let tests =
    testList "All" [
        testCase "test" ignore
    ]
