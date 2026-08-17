// ${KIND:Overrides}
// ${SELECT0:set_P(System.Int32):System.Void}

[<AbstractClass>]
type A() =
    abstract P: int with set

type B() ={caret}
    inherit A()
