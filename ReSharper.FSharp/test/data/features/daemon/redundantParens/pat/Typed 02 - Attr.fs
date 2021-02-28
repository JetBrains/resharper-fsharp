module Module

let f1 ([<A>] a: int) = ()
let f2 ([<A>] (a: int)) = ()
let f3 (([<A>] a): int) = ()
let f4 ((([<A>] a): int)) = ()
