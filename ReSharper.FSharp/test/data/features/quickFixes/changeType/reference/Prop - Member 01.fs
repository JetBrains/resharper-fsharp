module Module

type T() =
    static member P = 1

let s: string = T.P{caret}
