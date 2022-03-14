// ${KIND:Overrides}
// ${SELECT0:P():System.Int32}

[<AbstractClass>]
type A() =
    abstract P: int with get, set

type B() ={caret}
    inherit A()
