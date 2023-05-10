type FooBuilder() =
    member _.Yield _ = 1

    [<CustomOperation "create">]
    member _.Create(i1: int, s: string, i2: int) = 1

FooBuilder() {
    create "" {caret}
}
