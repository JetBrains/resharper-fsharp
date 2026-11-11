namespace Ns1

type A() =
    inherit System.Attribute()

namespace Ns2

module M =
    [<A{caret}>]
    let a = ()
