// ${FILTER_ITEMS:X,Y}
type Z() =
    member val X = 0 with get, set
    member val Y = "" with get, set

let z = Z({caret}, Y = "")
