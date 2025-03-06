do
    match () with
    | a | b -> ()

    match () with
    | a
    | b -> ()

    match () with
    | a
    | b
    | c -> ()
    | d -> ()

    match () with
    | a | b
    | c -> ()

    match () with
    | a -> ()

    match () with
    | a | b
    | c -> ()

    f (function
        | a -> ())

    match () with
    | [] -> ()
    | { F: 1 } -> ()
    | A(1, 2, 3,
        4, 5, 6) -> ()
