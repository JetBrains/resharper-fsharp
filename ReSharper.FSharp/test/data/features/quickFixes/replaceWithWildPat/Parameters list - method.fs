//${RUN:1}
type A() =
    member _.Method(x, {caret}y, z) =
        let a = 1
        x
