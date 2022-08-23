module Module

type T() =
    let p = 1
    do p |> ignore

    member val P = 1 with get, set
    static member M() = T()

T.M(P = 1)
