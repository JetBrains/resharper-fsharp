type T(a: int) =
    new () = T()
    new(a) = T()
    new(a, b) = T()

    new (a) = T()
    new (a, b) = T()

    member _.M() = ()

    member _.M(a) = ()
    member _.M (a) = ()

    member _.M(d: int) = ()
    member _.M (e: int) = ()
