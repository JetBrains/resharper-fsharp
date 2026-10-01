// ${ABSENT_ITEM:P}
module Module

[<Interface>]
type ITest =
    abstract P: int with get, set

let a =
    { new ITest with
        member x.P with set value = ()
        member x.P = 1
        member x.{caret} }
