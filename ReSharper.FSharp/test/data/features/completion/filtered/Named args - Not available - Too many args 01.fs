// ${FILTER_ITEMS:a}
type T() =
    member this.M(a: int) = ()

let t = T()
t.M(1, {caret})
