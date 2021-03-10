module Module

type S =
    struct
        val a: (int -> int)
        val b: int -> (int -> int)
        val c: int -> int -> (int -> int)

        val d: ((int -> int))
        val e: int -> ((int -> int))
        val f: int -> int -> ((int -> int))

        val g: (int -> (int -> int))
    end
