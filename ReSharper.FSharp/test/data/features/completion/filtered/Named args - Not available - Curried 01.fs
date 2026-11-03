// ${FILTER_ITEMS:b}
type T() =
    member this.M (a: int) (b: int) = ()

let t = T()
t.M (1) ({caret})
