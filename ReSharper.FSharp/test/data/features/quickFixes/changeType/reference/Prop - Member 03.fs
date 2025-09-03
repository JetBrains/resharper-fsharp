module Module

type T() =
    static member P: int = 1

let s: string = T.P{caret}
