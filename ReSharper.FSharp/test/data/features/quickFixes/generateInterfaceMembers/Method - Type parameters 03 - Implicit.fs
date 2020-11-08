type I =
    abstract M1: p: int -> 'a

    abstract M2: int -> 'a
    abstract M2: double -> 'a

type T() =
    interface I{caret}
