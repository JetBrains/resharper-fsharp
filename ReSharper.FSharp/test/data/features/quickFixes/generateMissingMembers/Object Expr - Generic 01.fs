module Module

type Base<'T>() =
    abstract M1: 'T -> unit
    abstract M2: 'T -> unit

{ new {caret}Base<int>() with
    override this.M1 _ = () }
