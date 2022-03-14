// ${KIND:Overrides}
// ${SELECT0:M(System.Int32):System.Void}
// ${SELECT1:M(System.Double):System.Void}

[<AbstractClass>]
type A() =
    abstract M: int -> unit

    abstract M: double -> unit
    default x.M(_: double) = ()

[<AbstractClass>]
type B() =
    inherit A()
    override x.M(_: int) = ()

type T() ={caret}
    inherit B()
