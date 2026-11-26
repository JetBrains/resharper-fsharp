module Module

open Expecto

type Tests() =
    [<Tests>]
    static member StaticTests =
        testList "Static" [
            testCase "test" <| fun _ -> ()
        ]

    [<Tests>]
    member _.InstanceTests =
        testList "Instance" [
            testCase "test" <| fun _ -> ()
        ]
