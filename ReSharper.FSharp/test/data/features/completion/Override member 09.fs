// ${COMPLETE_ITEM:override M(int)}
module Module

[<AbstractClass>]
type Base<'T>() =
    abstract M: 'T -> unit
    abstract M: string -> unit

type A() =
    inherit Base<int>()

    override this.M(s: string) = ()
    {caret}
