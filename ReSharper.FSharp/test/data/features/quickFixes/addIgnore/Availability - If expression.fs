module Test

type A() =
    member _.M() = 1
    member x.M1() =
        if true then 
            x.M()
