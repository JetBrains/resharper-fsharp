module Module

type I =
    member P1: (int * int)
    member P2: p: (int * int)

    member M1: (int * int) -> int
    member M2: p: (int * int) -> int

    member M3: p: (int * int) -> (int * int)
    member M4: p: (int * int) -> [<A>] (int * int)
