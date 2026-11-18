module Module

do
    match a with
    | 1 -> f(
               a
           )
    | _ -> f(
               a
           )

    match a with
    | 1 ->
        f(
            a
        )
    | _ -> ()

    try
        ()
    with
    | A -> f(
               a
           )
    | _ -> ()
