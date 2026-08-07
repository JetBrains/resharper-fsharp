// ${COMPLETE_ITEM:Dispose()}
module Module

[<AbstractClass>]
type Base() =
    abstract M: int -> unit

type A() =
    inherit Base()

    interface System.IDisposable with
        member this.{caret}

    override this.M(x) = ()

