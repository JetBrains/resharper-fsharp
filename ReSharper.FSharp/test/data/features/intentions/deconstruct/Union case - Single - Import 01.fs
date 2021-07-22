namespace Ns1

type U =
    | A of field: int

namespace Ns2

module Module =
    let a{caret} = Ns1.U.A 1
