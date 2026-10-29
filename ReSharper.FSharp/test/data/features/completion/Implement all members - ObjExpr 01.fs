// ${COMPLETE_ITEM:Implement IFoo}
module Module

type IFoo =
    abstract M: int -> int
    abstract P: string

{ new IFoo with
    {caret} }
