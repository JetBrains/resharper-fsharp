namespace Ns1

module Top =
    let [<Literal>] Literal1 = 1

namespace Ns2

module M =
    let i = Literal1{caret}
