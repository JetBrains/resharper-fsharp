module Module

type T() =
    member this.P = 1

let t = T()
let s: string = t.P{caret}
