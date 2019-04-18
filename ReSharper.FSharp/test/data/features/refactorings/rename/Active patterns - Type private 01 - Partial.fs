//${NEW_NAME:Zzz}
module Module

type T() =
    let (    | B  |    _ | ) x =
        if x then Some () else None

    do
        match true with
        | B{caret} | _ -> ()

    member x.Foo =
        match true with
        | B | _ -> ()
