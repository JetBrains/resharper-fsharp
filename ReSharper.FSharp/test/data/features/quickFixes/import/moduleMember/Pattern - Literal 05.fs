namespace Ns1

module Top =
    module Nested =
        let [<Literal>] Literal1 = 1

    match 1 with
    | Literal1{caret} -> ()
