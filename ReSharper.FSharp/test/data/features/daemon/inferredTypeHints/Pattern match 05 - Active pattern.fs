let (|Parse|_|) pattern input =
    if input = pattern then Some [1; 2; 3]
    else None

let parse =
    function
    | Parse "ints" (x::xs) -> x + List.sum xs
    | _ -> 0
