module Main

open Expecto

[<Tests>]
let allTests = testList "All" [ Lists.firstTests ]

[<Tests>]
let otherTests = testList "Other" [ Lists.secondTests ]
