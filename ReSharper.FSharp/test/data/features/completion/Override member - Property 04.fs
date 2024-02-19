// ${COMPLETE_ITEM:override P with set(int)}
module Foo

[<AbstractClass>]
type Base() =
    abstract P: int with get, set

type A() =
    inherit Base()

    {caret}
