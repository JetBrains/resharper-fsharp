namespace Ns1

module Top =
    let [<Literal>] internal Literal = 1

namespace Ns2

module M =
    let i = Literal{caret}
