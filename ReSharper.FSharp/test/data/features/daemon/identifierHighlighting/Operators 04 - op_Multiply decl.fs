module Module

type T =
    static member (*) (_, _) = ()

let (*) (_, _) = 0

(*)

1 * 1
