module Module = 
    type Type<'T>() =
        static member P = 1

    let t = Type<int>()
    t.P{caret}
