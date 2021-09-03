//${NEW_NAME:Zzz}
module Module

type T() =
    let (    | B  |    _ | ) x =
        if x then Some () else None

    let _ = (|B|_|)

    do
        match true with
        | B{caret} | _ -> ()

        let _ = (|B|_|)

    member x.Foo =
        match true with
        | B | _ -> ()

        let (|Id|) f x = x

        match () with
        | Id (|B|_|) 1 -> ()
