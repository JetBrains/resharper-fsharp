namespace Ns1

type A = { Field: int }

namespace Ns2

type A() =
    inherit System.Attribute()

namespace Ns3

module M =
    [<A{caret}>]
    let a = ()
