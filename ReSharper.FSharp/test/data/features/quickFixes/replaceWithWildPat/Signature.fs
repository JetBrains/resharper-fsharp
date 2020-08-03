//${RUN:1}
module Module

type Pattern = Pattern of a: int * b : int * c: int

let _ =
    match Pattern(5, 3, 4) with
    | Pattern({caret}a, b, c) when b > 5 -> ()
    | _ -> ()
