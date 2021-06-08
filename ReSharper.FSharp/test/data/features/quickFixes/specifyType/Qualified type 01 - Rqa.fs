[<RequireQualifiedAccess>]
module M =
    type T() =
        member _.P = 1

let f (t: M.T) = ()

let g t =
    t.P{caret}
    f t
