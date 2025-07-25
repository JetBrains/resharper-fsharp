module M

type S =
    struct
        val Field: int
    end

let mutable s = S()

s.Field{caret} <- 1
