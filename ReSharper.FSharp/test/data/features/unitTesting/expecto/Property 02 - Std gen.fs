module Module

open Expecto

[<Tests>]
let tests =
    testList "Properties" [
        testPropertyWithConfigs FsCheckConfig.defaultConfig FsCheckConfig.defaultConfig "configs" <| fun (a: int) -> a = a
        testPropertyWithConfigStdGen (1UL, 2UL, None) FsCheckConfig.defaultConfig "std gen" <| fun (a: int) -> a = a
        etestProperty (1UL, 2UL, None) "etest" <| fun (a: int) -> a = a
        etestPropertyWithConfig (1UL, 2UL, None) FsCheckConfig.defaultConfig "etest config" <| fun (a: int) -> a = a
    ]
