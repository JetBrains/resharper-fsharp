[<AbstractClass>]
type A<'T>() =
    abstract M1: 'T list -> unit
    abstract M2: int -> unit

type {caret}B() =
    inherit A<int>()

    override this.M1 _ = ()
