// ${FILTER_ITEMS:P,Q}
type A() =
    inherit System.Attribute()
    member val P = 0 with get, set
    member _.Q with get () = 0 and private set (_: int) = ()

[<A({caret})>]
let x = 1
