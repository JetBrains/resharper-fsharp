module Module

type T() =
    let (    |  B  |    C | ) x =
        if x then B else C

    do
        match true with
        | B{caret}
        | C -> ()

    member x.Foo =
        match true with
        | B
        | C -> ()
