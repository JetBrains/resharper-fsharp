module Module

type S() =
    static val F: int
 
let s = S()
s.F{caret}
