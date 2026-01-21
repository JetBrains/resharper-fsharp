module Tuples

let f1 x =
    fst x + snd x

let f2 (a, b) =
    a + b


let run () =
    let t = 1, 2

    f1 t
    f1 (1, 2)
    f2 t
    f2 (1, 2)
