// ${COMPLETE_ITEM:P}
module Module

[<AbstractClass>]
type Base() =
    abstract P: int with get, set

type A() =
    inherit Base()

    override this.P with set value = failwith "todo"
    override this.{caret}
