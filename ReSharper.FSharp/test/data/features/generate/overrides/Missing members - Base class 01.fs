// ${KIND:Implementations}
// ${SELECT0:get_P2():System.Int32}
// ${SELECT1:get_P1():System.Int32}

[<AbstractClass>]
type A() =
    abstract P2: int
    abstract P1: int

type B() ={caret}
    inherit A()
