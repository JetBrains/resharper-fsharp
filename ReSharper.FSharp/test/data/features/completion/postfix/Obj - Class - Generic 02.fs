// ${COMPLETE_ITEM:new}
module Module

[<AbstractClass>]
type Base<'T>() =
    abstract M: 'T -> unit
    abstract M: string -> unit

Base<int>().{caret}
