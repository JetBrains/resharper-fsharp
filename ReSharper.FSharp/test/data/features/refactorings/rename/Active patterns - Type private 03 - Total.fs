//${NEW_NAME:Zzz}
module Module

type T() =
    let (    |  B  |    C | ) x =
        if x then B else C

    let _ = (|B|C|)

    do
        match true with
        | B{caret}
        | C -> ()

        let _ = (|B|C|)

    member x.Foo =
        match true with
        | B
        | C -> ()

        let (|Id|) f x = x

        match () with
        | Id (|B|C|) 1 -> ()
