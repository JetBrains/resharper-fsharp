module Module

open Expecto

[<Tests>]
let tests =
    testList "Mapped" (
        List.map2 testCase ["a"; "b"] [ ignore; ignore ])
