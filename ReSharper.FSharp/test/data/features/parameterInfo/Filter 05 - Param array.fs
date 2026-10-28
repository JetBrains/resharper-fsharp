type T() =
    member this.P([<System.ParamArray>] xs: int[]) = ()
    member this.P(a: string) = ()

let t = T()
t.P(1, 2, 3, {caret})
