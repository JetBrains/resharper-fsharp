type Pattern = Pattern of a: int * b : int * c: int

let _ =
    match Pattern(5, 3, 4) with
    | Pattern(a, b, c) when b > 5 -> ()
    | _ -> ()

    match Pattern(5, 3, 4) with
    | Pattern({caret}d, e, f) when f > 5 -> ()
    | _ -> ()
