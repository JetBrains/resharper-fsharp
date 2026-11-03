// ${FILTER_ITEMS:a,P}
type A(a: int) =
    inherit System.Attribute()
    member val P = 0 with get, set

[<A({caret})>]
let x = 1
