module Module

open Expecto

[<Tests>]
let piped =
    [ testCase "first" <| fun _ -> ()
      testCase "second" <| fun _ -> () ]
    |> testList "Piped"
    |> testSequenced

[<Tests>]
let wrapped =
    testSequencedGroup "group" (
        testList "Wrapped" <| [
            testCase "test" <| fun _ -> ()
        ])

[<Tests>]
let concatenated =
    testList "Concatenated" (
        [ testCase "first" <| fun _ -> () ] @
        [ testCase "second" <| fun _ -> () ])

[<Tests>]
let consed =
    testList "Consed" (
        (testCase "first" <| fun _ -> ()) :: [ testCase "second" <| fun _ -> () ])

[<Tests>]
let chained =
    testList "Chained" (
        (testCase "first" <| fun _ -> ()) ::
        [ testCase "second" <| fun _ -> () ] @
        [ testCase "third" <| fun _ -> () ] @
        [ testCase "fourth" <| fun _ -> () ])
