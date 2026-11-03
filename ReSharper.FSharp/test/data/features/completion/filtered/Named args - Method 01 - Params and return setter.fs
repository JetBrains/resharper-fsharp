// ${FILTER_ITEMS:a,b,X}
type Z() =
    member val X = 0 with get, set

type T() =
    member this.M(a: int, b: string) = Z()

let t = T()
t.M({caret})
