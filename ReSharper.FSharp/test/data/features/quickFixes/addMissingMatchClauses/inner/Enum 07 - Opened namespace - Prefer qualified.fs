namespace Ns1

type E =
    | A = 1
    | B = 2

namespace Ns2

module Module =
    open Ns1

    match E.A{caret} with
    | E.A -> ()
