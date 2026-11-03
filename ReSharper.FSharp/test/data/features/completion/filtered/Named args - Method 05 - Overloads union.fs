// ${FILTER_ITEMS:a,b,c}
type T() =
    member this.M(a: int, b: string) = ()
    member this.M(a: int, c: float) = ()

let t = T()
t.M(1, {caret})
