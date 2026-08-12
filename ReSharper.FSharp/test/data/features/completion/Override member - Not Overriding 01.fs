// ${ABSENT_ITEM:P}
module Module

[<AbstractClass>]
type Base() =
    abstract P: int
    default this.P = 1

type A() =
    inherit Base()

    member this.{caret}

