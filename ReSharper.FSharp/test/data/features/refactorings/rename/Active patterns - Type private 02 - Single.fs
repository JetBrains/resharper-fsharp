//${NEW_NAME:Zzz}
module Module

type T() =
    let (    | Not  | ) x =
        not x

    let f (Not x) =
        let (Not{caret} x) = x
        x

    let _ = (|Not|)

    do
        match true with
        | Not x -> ()

        let _ = (|Not|)

    member x.Foo =
        match true with
        | Not x -> ()

        let (|Id|) f x = x

        match () with
        | Id (|Not|) 1 -> ()
