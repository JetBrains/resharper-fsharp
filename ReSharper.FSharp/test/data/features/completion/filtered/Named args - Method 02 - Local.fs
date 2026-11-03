// ${FILTER_ITEMS:a,b,t}
type T() =
    member this.M(a: int, b: string) = ()

let a = 1

let f () =
    let b = ""
    let t = T()
    t.M({caret})
