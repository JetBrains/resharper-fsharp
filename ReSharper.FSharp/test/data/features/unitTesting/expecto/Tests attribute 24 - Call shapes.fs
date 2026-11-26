module Module

open System
open Expecto
open Expecto.CSharp

[<Tests>]
let tupleTests =
    ("Tuple", [ testCase "piped" <| fun _ -> () ]) ||> testList

[<Tests>]
let methodTests =
    Runner.TestList("Methods", [
        Runner.TestCaseA("positional", Action(fun () -> ()))
    ])

[<Tests>]
let qualifiedTests =
    Expecto.Tests.testList "Qualified" [
        Expecto.Tests.testCase "qualified" ignore
    ]
