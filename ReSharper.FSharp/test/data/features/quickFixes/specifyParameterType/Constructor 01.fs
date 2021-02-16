type T(x) =
    member _.M() =
        x.Contains{caret}("")
        "".Insert(1, x)
