// ${COMPLETE_ITEM:Implement IBar}
module Module

type IFoo =
    abstract M: int -> int

type IBar =
    abstract N: string -> unit
    abstract Q: int

{ new IFoo with
    member this.M(x) = x
  interface IBar with
    {caret} }
