namespace Ns1.Ns2

type T() = class end

[<AbstractClass>]
type A() =
    abstract M: int -> unit
    abstract M: T -> unit

namespace Ns1.Ns3

type {caret}B() =
    inherit Ns1.Ns2.A()
