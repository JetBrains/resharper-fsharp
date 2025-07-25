module M

type S =
    struct
        val mutable Field: int
    end

let s = S()


{caret}s.Field <- 1
