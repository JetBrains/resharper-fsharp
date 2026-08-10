// ${COMPLETE_ITEM:P with set(int)}
module Module

[<Interface>]
type IBase() =
    abstract P: int with get, set

type A() =
    interface IBase with
        override this.P = 1
        override this.{caret}
