// ${COMPLETE_ITEM:member Dispose()}
module Module

[<AbstractClass>]
type Base() =
    abstract M: int -> unit

type A() =
    inherit Base()

    interface System.IDisposable with
        {caret}

    override this.M(x) = ()

