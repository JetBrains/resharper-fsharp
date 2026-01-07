module FunctionsModule

let f1 () =
    1

let f2 x =
    x + 1

let f3 a b =
    a + b

let f4 a b =
    a + 1

let run () =
    let _ = f1 ()
    let _ = f2 1
    let _ = f3 1 2
    let _ = f4 1 2

    ()
