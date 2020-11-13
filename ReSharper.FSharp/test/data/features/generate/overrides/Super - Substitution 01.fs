// ${KIND:Overrides}
// ${SELECT0:M(System.Int32):System.Void}
// ${SELECT1:M(T):System.Void}

[<AbstractClass>]
type A<'T>() =
    abstract M: int -> unit
    abstract M: 'T -> unit

type T() ={caret}
    inherit A<double>()
