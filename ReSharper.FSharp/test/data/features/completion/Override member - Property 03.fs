// ${COMPLETE_ITEM:override P with set(int)}
module Module

[<AbstractClass>]
type Base() =
    abstract P: int with get, set
    default this.P = 1
    default this.P with set value = ()

type A() =
    inherit Base()

    {caret}
