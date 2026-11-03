// ${FILTER_ITEMS:a,Settable,Settable0}
type A0() =
    member val Settable0 = 0 with get, set

type A() =
    member val Settable = 0 with get, set

type T() =
    member this.Run(): A0 = A0()
    member this.Run(a: int): A = A()

let t = T()
t.Run({caret})
