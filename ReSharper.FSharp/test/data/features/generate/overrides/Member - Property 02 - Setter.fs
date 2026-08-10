// ${KIND:Overrides}
// ${SELECT0:get_P():System.Int32}
// ${SELECT1:set_P(System.Int32):System.Void}

[<AbstractClass>]
type A() =
    abstract P: int with get, set

type B() ={caret}
    inherit A()
