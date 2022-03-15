// ${KIND:Overrides}
// ${SELECT0:P():System.Int32}

[<AbstractClass>]
type A() =
    abstract P: int with set

type B() ={caret}
    inherit A()
