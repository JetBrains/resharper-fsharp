module Module

type T() =
    static member val P = 1

let s: string = T.P{caret}
