module Module

open Expecto

[<Tests>]
let generated =
    testList "Generated" [
        for i in 1 .. 3 ->
            testCase "generated" <| fun _ -> ()
    ]

[<Tests>]
let mapped =
    testList "Mapped" ([ 1; 2 ] |> List.map (fun i -> testCase "mapped" <| fun _ -> ()))

[<Tests>]
let interpolated =
    testList "Interpolated" [
        testCase $"interpolated" <| fun _ -> ()
    ]

[<Tests>]
let nestedInTest =
    testCase "outer" <| fun _ ->
        let inner = testCase "inner" <| fun _ -> ()
        ()
