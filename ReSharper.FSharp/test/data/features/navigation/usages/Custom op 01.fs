module Module

type FooBuilder() =
    member _.Yield _ = 1

    [<CustomOperation "create">]
    member _.Create{on}(i1: int, s: string, i2: int) = 1

FooBuilder() {
    create "" 1
} |> ignore
