module Module

type IA<'T> =
    abstract M2: 'T -> unit
    abstract M1: 'T -> unit

type IB<'U> =
    inherit IA<'U>
    inherit IA<string>

type T<'a>() =
    interface IB{caret}<'a>
