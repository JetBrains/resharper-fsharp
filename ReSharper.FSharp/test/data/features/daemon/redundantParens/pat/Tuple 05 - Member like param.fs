module Module

type T1() =
    new ((a, b)) = T()

    member _.M((a, b)) = ()

    member _.P with get ((a, b)) = ()

type T2() =
    new (((a, b))) = T()

    member _.M(((a, b))) = ()

    member _.P with get (((a, b))) = ()

type T3() =
    new ((((a, b)))) = T()

    member _.M((((a, b)))) = ()

    member _.P with get ((((a, b)))) = ()

