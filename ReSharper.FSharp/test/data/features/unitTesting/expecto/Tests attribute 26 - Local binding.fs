module Module

open Expecto

[<Tests>]
let tests =
    let local = testCase "local" <| fun _ -> ()

    testList "All" [
        local
        yield! [ 1; 2 ] |> List.map (fun i -> testCase "mapped" <| fun _ -> ())
    ]
