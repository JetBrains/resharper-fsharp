module Module

type I =
    abstract M: unit -> int

let i: I = Unchecked.defaultof<_>
let s: string = i.M(){caret}
