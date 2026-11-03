// ${FILTER_ITEMS:a,b,X}
type Z(a: int, b: string) =
    member val X = 0 with get, set

let z = Z({caret})
