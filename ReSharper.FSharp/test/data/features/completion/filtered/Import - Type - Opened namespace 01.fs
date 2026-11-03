// ${FILTER_ITEMS:T}
namespace Ns

type T() = class end

namespace Ns2

open Ns

module M =
    let x = {caret}
