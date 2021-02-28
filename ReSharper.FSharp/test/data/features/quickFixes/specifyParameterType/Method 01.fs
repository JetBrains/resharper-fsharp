type T() =
    static member M(x) =
        x.Contains{caret}("")
        "".Insert(1, x)
