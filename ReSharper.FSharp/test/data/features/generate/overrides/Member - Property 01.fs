// ${KIND:Overrides}
// ${SELECT0:P():System.Int32}

type A() =
    abstract P: int
    default x.P = 1

type T() ={caret}
    inherit A()
