// ${COMPLETE_ITEM:new}
module Module

type IBase<'T> =
    abstract M: string -> unit
    abstract M: 'T -> unit

IBase<int>.{caret}
