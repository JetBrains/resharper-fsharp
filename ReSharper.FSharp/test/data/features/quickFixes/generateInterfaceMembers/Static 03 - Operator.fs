module Module

type I<'T when 'T :> I<'T>> =
    static abstract P: int
    static abstract (+): 'T * 'T -> 'T
    abstract M: unit -> unit

type T() =
    interface I{caret}<T>
