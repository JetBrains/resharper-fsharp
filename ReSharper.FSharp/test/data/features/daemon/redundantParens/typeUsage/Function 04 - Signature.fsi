module Module

type I =
    abstract AM1: (int -> int)
    abstract AM2: (int -> (int -> int))
    abstract AM3: int -> (int -> int) -> int

    member P1: (int -> int)
    member P2: (int -> (int -> int))

    member M1: int -> (int -> int) -> int

val a: (int -> int)
val b: int -> (int -> int)
val c: int -> int -> (int -> int)

val d: ((int -> int))
val e: int -> ((int -> int))
val f: int -> int -> ((int -> int))

val g: (int -> (int -> int))
