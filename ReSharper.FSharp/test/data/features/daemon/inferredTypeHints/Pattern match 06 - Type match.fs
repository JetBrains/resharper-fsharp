let parse (o: obj) =
    match o with
    | :? int as i -> i + 5
    | _ -> 0
