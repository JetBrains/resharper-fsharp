module Module

type T =
    static member M() = 1

let s: string = T.M(){caret}
