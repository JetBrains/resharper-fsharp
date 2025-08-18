module Module

type S =
    struct
        val Field: int
    end

let s = S()

let f (p: string) = ()

f s.Field{caret}
