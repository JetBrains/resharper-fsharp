// ${COMPLETE_ITEM:B (in Ns.M.U)}

namespace Ns

module M =
    type U =
        | A
        | B of int

namespace Ns2

open Ns

module M2 =
    let x = {caret}
