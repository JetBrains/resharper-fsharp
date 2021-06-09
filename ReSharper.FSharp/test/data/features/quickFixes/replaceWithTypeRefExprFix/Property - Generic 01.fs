module Module =
    type Type<'T>() =
        static member P = 1

    let t = Type()
    t.P{caret}
