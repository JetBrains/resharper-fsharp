module Module

type T() =
    static member M1(x) =
        T.M1(x + 1)

    static member M2(x) =
        T.M2(x + 1)
        ()

    static member M3(x) =
        T.M3
        ()

    static member M4(a, b) =
        T.M4 a
        ()

    static member M5(a, b) =
        T.M5 a

    static member M6(a, b) =
        T.M6
