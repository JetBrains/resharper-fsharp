module Module

type T() =
    static member val P: int = 1

let s: string = T.P{caret}
