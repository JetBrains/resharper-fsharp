module Module

type T() =
    static member M(x) =
        T.M(x + 1)
