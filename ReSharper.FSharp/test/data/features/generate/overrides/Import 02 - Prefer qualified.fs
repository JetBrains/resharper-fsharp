// ${KIND:Overrides}
// ${SELECT0:M(System.Int32):System.Void}
// ${SELECT1:M(Ns1.T):System.Void}

namespace Ns1

type T() = class end

[<AbstractClass>]
type A() =
    abstract M: int -> unit
    abstract M: T -> unit

namespace Ns2

type B() ={caret}
    inherit Ns1.A()
