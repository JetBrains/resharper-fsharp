module Module

open Expecto

let createTests (): Test list = failwith "The tests come from another assembly"

[<Tests>]
let tests = testList "All" (createTests ())
