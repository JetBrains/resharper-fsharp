module Module

type T =
    static member M(): int = 1

let s: string = T.M(){caret}
