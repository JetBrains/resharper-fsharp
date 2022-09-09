module Module

"".[1..2, 1]
"".[1 .. 2, 3 .. 4]


open System.Collections.Generic

let l = List([1])

l[1 .. 2]
l[1 .. 2] <- 1
l[1 .. 2, 3 .. 4]
