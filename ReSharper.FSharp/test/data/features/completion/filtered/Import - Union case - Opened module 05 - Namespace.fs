// ${FILTER_ITEM:B}
namespace Ns

module M =
    type U =
        | A
        | B of int

namespace Ns2

open Ns.M

module M2 =
    let x = {caret}
