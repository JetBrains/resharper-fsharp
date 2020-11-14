// ${KIND:Overrides}
// ${SELECT0:M(System.Int32):System.Void}
// ${SELECT1:M(System.Double):System.Void}

[<AbstractClass>]
type A() =
    abstract M: int
    default x.M(_: int) = ()

    abstract M: double
    default x.M(_: double) = ()

type B() =
    inherit A()
    override x.M(_: int) = ()

type T() ={caret}
    inherit B()
