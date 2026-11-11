namespace Ns1

type U =
    | A
    | B
    | C

namespace Ns2

module Module =
    open Ns1

    match A{caret} with
    | A -> ()
