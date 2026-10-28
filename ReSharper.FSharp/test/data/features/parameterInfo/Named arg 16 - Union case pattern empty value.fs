type U =
    | A of x: int * y: int * z: int

let f u =
    match u with
    | A(y = {caret}) -> ()
    | _ -> ()
