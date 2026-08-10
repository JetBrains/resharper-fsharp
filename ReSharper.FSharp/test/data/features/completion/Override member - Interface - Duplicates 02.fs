// ${ABSENT_ITEM:P}
module Module

[<Interface>]
type ITest =
    abstract member P: int with get, set

type A() =
    interface ITest with
        member this.P = 1
        member this.P with set v = ()

        member this.{caret}
