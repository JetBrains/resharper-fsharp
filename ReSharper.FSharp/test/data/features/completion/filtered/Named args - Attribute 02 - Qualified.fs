// ${FILTER_ITEMS:a,P}
module M =
    type A(a: int) =
        inherit System.Attribute()
        member val P = 0 with get, set

[<M.A({caret})>]
let x = 1
