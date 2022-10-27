namespace Ns1

type R =
    { Field: int }

namespace Ns2

module M =
    let f (r: Ns1.R) =
        ()

    type R2 =
        { Field: string }

    f { {caret} }
