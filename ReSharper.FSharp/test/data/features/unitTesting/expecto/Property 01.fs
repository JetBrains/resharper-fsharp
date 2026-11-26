module Module

open Expecto

[<Tests>]
let tests =
    testList "Properties" [
        testProperty "addition" <| fun (a: int) (b: int) -> a + b = b + a
        testPropertyWithConfig FsCheckConfig.defaultConfig "with config" <| fun (a: int) -> a = a
        ptestProperty "pending" <| fun (a: int) -> a = a
    ]
