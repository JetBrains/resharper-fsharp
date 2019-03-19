module Module

type T() =
    let (    | Not  | ) x =
        not x

    let f (Not x) =
        let (Not{caret} x) = x
        x

    do
        match true with
        | Not x -> ()

    member x.Foo =
        match true with
        | Not x -> ()
