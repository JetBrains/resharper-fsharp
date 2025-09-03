module Module

type T() =
    member val P = 1

let t = T()
let s: string = t.P{caret}
