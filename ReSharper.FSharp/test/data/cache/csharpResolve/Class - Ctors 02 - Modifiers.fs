module Module

type T1 internal (x: int) =
    new () = T(123)

type T2 (x: int) =
    internal new () = T(123)
