type T() =
    member this.M(a: int, b: string) = ()

let t = T()
t.M(1, a = 2, {caret})
