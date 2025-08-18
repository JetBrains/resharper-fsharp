module Module

type S =
    struct
        val Field: int
    end

let s = S()
s.Field <- ""{caret}
