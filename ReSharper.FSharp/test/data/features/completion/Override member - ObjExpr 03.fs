// ${COMPLETE_ITEM:override M(int)}
module Module

[<AbstractClass>]
type Base() =
    abstract P: int
    abstract M: int -> unit

{ new Base() with
    override this.P = 1
    {caret} }
