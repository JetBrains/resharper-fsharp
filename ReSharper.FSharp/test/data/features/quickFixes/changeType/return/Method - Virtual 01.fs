module Module

type T() =
    abstract M: unit -> int
    default this.M() = 1

let t = T()
let s: string = t.M(){caret}
