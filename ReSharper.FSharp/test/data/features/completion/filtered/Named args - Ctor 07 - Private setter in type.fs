// ${FILTER_ITEMS:P,Q}
type A() =
    member val P = 0 with get, set
    member val Q = 0 with get, private set

    static member Create() = A({caret})
