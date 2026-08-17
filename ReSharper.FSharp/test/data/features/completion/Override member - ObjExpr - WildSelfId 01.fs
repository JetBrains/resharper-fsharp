// ${COMPLETE_ITEM:M(int)}
module Module

[<AbstractClass>]
type Base<'T>() =
    abstract M: 'T -> unit
    abstract M: string -> unit

let x =
    { new Base<int>() with
        override this.M(s: string) = ()
        override _.{caret} }

