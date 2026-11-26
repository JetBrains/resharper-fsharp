module Module

open Expecto

[<Tests>]
let private privateTests =
    testList "Private" [
        testCase "private binding" <| fun _ -> ()
    ]

module internal InternalTests =
    [<Tests>]
    let tests =
        testList "Internal" [
            testCase "internal module" <| fun _ -> ()
        ]

[<Tests>]
let tests =
    testList "Public" [
        testCase "public binding" <| fun _ -> ()
    ]
