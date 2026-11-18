let f a items =
    if{caret} a then
        let xs = items |> List.map (fun x ->
            x + 1)

        let ys =
            xs |> List.filter (fun x -> x > 1)


        if List.isEmpty ys then [] else
        ys
    else
        []
