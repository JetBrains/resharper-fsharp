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

    match a with
    | x when f(
                 a
             ) -> 1
    | _ -> 2

    match a with
    | 1 -> fun x -> f(
                        a
                    )
    | _ -> id
