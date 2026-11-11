namespace Ns1

module Module1 =
    module Nested =
        type U =
            | A
            | B
            | C

namespace Ns2

module Module2 =
    open Ns1.Module1.Nested

    match A{caret} with
    | A -> ()
