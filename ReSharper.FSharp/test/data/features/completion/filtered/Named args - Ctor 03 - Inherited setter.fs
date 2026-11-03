// ${FILTER_ITEMS:P}
type B() =
    member val P = 0 with get, set

type Z() =
    inherit B()

let z = Z({caret})
