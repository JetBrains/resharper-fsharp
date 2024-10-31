module Module

type T() =
    member this.M1(x) =
        this.M1(x + 1)

    member this.M2(x) =
        this.M2(x + 1)
        ()

    member this.M3(x) =
        this.M3
        ()

    member this.M4(a, b) =
        this.M4 a
        ()

    member this.M5(a, b) =
        this.M5 a

    member this.M6(a, b) =
        this.M6
