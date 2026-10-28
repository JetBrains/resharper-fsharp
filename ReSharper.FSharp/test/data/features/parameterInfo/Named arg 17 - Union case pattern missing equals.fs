type U =
    | A of x: int * y: int * z: int

let f u =
    match u with
    | A(y = 1; x{caret}) -> ()
    | _ -> ()
