let x =
    match [1;2] with
    | x -> ()
    | [x] -> ()
    | [x; y] -> ()
    | x::y -> ()
    | x::y::z -> ()
    | _ -> ()
