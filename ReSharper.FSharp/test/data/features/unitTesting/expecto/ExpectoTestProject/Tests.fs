module ExpectoTestProject.Tests

open Expecto

let parserTests =
    testList "Parser" [
        testCase "numbers" <| fun _ ->
            Expect.equal (int "1") 1 "number"

        test "strings" {
            Expect.equal (string 1) "1" "string"
        }
    ]

[<Tests>]
let tests =
    testList "All" [
        parserTests

        testCase "passes" <| fun _ ->
            printfn "output of passes"
            Expect.isTrue true "true"

        testCase "fails" <| fun _ ->
            Expect.equal 1 2 "numbers"

        ptestCase "pending" <| fun _ -> ()

        testCase "throws" <| fun _ ->
            failwith "error"

        testList "Nested" [
            testCaseAsync "async" <| async { return () }
        ]

#if !NO_TEST_THEORY
        testTheory "theory" [1; 2] <| fun x ->
            Expect.isGreaterThan x 0 "positive"
#endif

        testCase "throws with inner" <| fun _ ->
            raise (System.InvalidOperationException("outer", System.ArgumentException("inner")))
    ]

let private secondTestStarted = new System.Threading.ManualResetEventSlim()

[<Tests>]
let parallelTests =
    testList "Parallel" [
        testCase "first" <| fun _ ->
            printfn "output of first"
            Expect.isTrue (secondTestStarted.Wait(5000)) "The second test starts before the first test ends"

        testCase "second" <| fun _ ->
            printfn "output of second"
            secondTestStarted.Set()
    ]

#if DUPLICATE_NAMES
[<Tests>]
let duplicateTests =
    testList "Duplicates" [
        testCase "same" <| fun _ -> ()
        testCase "same" <| fun _ -> ()
    ]
#endif

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
let listTests =
    [ testCase "in list" <| fun _ -> () ]

#if FOCUSED
[<Tests>]
let focusedTests =
    testList "Focused" [
        ftestCase "focused" <| fun _ -> ()
        testCase "other" <| fun _ -> ()
    ]
#endif

[<Tests>]
let dotTests =
    testList "Dots" [
        testList "Api.V1" [
            testCase "get" <| fun _ -> ()
        ]

#if DUPLICATE_DOT_NAMES
        testList "Api" [
            testList "V1" [
                testCase "get" <| fun _ ->
                    failwith "nested get"
            ]
        ]
#else
        testList "Api" [
            testCase "V1" <| fun _ -> ()
        ]
#endif
    ]

#if THROWING_ROOT
[<Tests>]
let testsNextToThrowingRoot =
    testCase "compiled with the throwing root" <| fun _ -> ()
#endif

module NestedTests =
    [<Tests>]
    let tests =
        testList "Nested module" [
            testCase "in nested module" <| fun _ -> ()
        ]

#if SAME_PATH
[<Tests>]
let samePathTests =
    testList "Same path" [
        testCase "B" <| fun _ -> ()

        testList "B" [
            testCase "c" <| fun _ -> ()
        ]
    ]
#endif
