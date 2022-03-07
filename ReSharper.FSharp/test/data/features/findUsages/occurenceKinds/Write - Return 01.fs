module Module

type T() =
    member val P = 1 with get, set
    static member M() = T()

T.M(P = 1)
