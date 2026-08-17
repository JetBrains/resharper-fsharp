// ${COMPLETE_ITEM:override M(int)}
module Module

[<AbstractClass>]
type Base() =
    abstract M: int -> unit

type A() =
    inherit Base()

    interface System.IDisposable with
        member this.Dispose() = ()

    {caret}

