namespace A

[<RequireQualifiedAccess>]
module M =
    type T() =
        member _.P = 1

namespace B

module B_1 =
    open A

    let f (t: M.T) = ()

module B_2 =
    open B_1

    let g t =
        t.P{caret}
        f t
